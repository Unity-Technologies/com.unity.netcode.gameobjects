#!/bin/bash

# DESCRIPTION--------------------------------------------------------------------------
  # This bash file is used to build and run CMB service resources.
  # These are resources relating to the distributed authority feature. The CMB service is built and maintained by the services team.

# CONTENTS-----------------------------------------------------------------------------
  # There are two resources that are built and run:
    # 1. The echo server - this is a "mock" server that simply echoes back the messages it receives. It is used to test message serialization.
      #  The echo server is used inside the DistributedAuthorityCodecTests. These are tests that are built and maintained by the CMB service team.
    # 2. The standalone server - this is a release build of the full CMB service.

# USAGE---------------------------------------------------------------------------------
  # The script requires ports to be defined. This works via the following command:
    # ./<path-to-script>/run_cmb_service.sh -e <echo-server-port> -s <cmb-service-port>

  # Example usage:
    # ./<path-to-script>/run_cmb_service.sh -e 7788 -s 7799

  # This script is currently used in the desktop-standalone-tests yamato job.

# TECHNICAL CONSIDERATIONS---------------------------------------------------------------
  # This is a bash script and so needs to be run on a Unix based system.
    # - It runs on mac and ubuntu bokken machines. It will not run on windows bokken machines.

  # This script starts processes running in the background. All logs are still written to stdout and will be seen in the logs of the job.
  # Running in the background means that the processes will continue to run after the script exits.
    # This is not a concern for bokken machines as all processes are killed when the machine is shut down.

# QUALITY THOUGHTS--------------------------------------------------------------------
  # Currently this script simply uses the head of the cmb service repo.
  # We might want to consider using the latest released version in the future.

#-------------------------------------------------------------------------------------

# Define error message if ports are not defined
ERROR="Error: Expected ports to be defined! Example script usage:"
EXAMPLE="run_cmb_service.sh -e <echo-server-port> -s <cmb-service-port>"

# get arguments passed to the script
while getopts 'e:s:l:' flag; do
case "${flag}" in
  e) echo_port="${OPTARG}" ;;
  s) service_port="${OPTARG}" ;;
  l) build_logs="${OPTARG}" ;;
  *) printf "%s\n" "$ERROR" "$EXAMPLE"
      exit 1 ;;
  esac
done

# ensure arguments were passed and the ports are defined
if [ -z "$echo_port" ] || [ -z "$service_port" ]; then
  printf "%s\n" "$ERROR" "$EXAMPLE";
  exit 1;
elif [[ "$echo_port" == "$service_port" ]]; then
  printf "Ports cannot be the same! Please use different ports.\n";
  exit 1;
fi

echo "Starting with echo server on port: $echo_port and the cmb service on port: $service_port"

# Setup -------------------------------------------------------------------------


ThrewError=false

# Mimics "Try-Catch" where you have to check if an error occurred after invoking
try() {
   ThrewError=false
  "$@" || throw "$@"
}

# Invoked by try
throw() {
    logError "An error occurred executing this command:$@"
    ThrewError=true
}

# A way to log messages that are easy to distinguish from the rest of the logs
logMessage(){
    printf "\n############################################\n"
    printf "$@\n"
    printf "############################################\n"
}

# A way to log error messages that are easy to distinguish from the rest of the logs
logError(){
    printf "\n!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n"
    printf "$@\n"
    printf "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n"
}

# Unity Version -----------------------------------------------------------------

# This is the path to the logs from the standalone build job
FILE="$build_logs/TestResults.js"

unity_version=$(sed -n 's/.*"editorVersion": *"\([^" (]*\).*/\1/p' $FILE)

# ensure arguments were passed and the ports are defined
if [ -z "$unity_version" ]; then
  logMessage "Failed to find unity version! Exiting...";
  exit 1;
else
  logMessage "Found Unity version: $unity_version";
fi

# Protocol Buffer Compiler ------------------------------------------------------

# Apply any updates
logMessage "Updating modules..."
sudo apt-get update

# Install Protocol Buffer Compiler (using apt-get)
logMessage "Installing protocol buffer compiler as SUDO..."
try sudo apt-get install -y protobuf-compiler

# If the previous command failed, try without sudo
if $ThrewError; then
logMessage "Installing protocol buffer compiler as shell assigned account..."
apt-get install -y protobuf-compiler
else
logMessage "Protocol buffer compiler was installed as sudo!"
fi

# Add the PROTOC environment variable that points to the Protocol Buffer Compiler binary
export PROTOC="/usr/bin/protoc"

# Validate the PROTOC env var by getting the protoc version
try $PROTOC --version

if $ThrewError; then
logError "Failed to properly run protoc!"
exit -1
else
logMessage "Protocol Buffer Compiler Installed & ENV variables verified!\n PROTOC path is: $PROTOC"
fi

# clone the cmb service repo
git clone https://github.com/Unity-Technologies/mps-common-multiplayer-backend.git

# navigate to the cmb service directory
cd ./mps-common-multiplayer-backend/runtime

# Install rust
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y

# Add the cargo bin directory to the PATH
export PATH="$HOME/.cargo/bin:$PATH"

# Echo server -------------------------------------------------------------------

# Build the echo server
logMessage "Beginning echo server build..."
cargo build --example ngo_echo_server

# Run the echo server in the background
logMessage "Running echo server tests..."
cargo run --example ngo_echo_server -- --port $echo_port &

# CMB Service -------------------------------------------------------------------

# Build a release version of the standalone cmb service
logMessage "Beginning service release build..."
cargo build --release --locked

# Run the standalone service on an infinite loop in the background.
# The infinite loop is required as the service will exit each time all connected clients disconnect.
# This means the service will exit after each test. The infinite loop will immediately restart the service each time it exits.
logMessage "Running service integration tests..."
echo "comb-server -l error --metrics-port 5000 standalone --port $service_port -t 60m --unity-version $unity_version"

while :; do
  ./target/release/comb-server -l error --metrics-port 5000 standalone --port $service_port -t 60m --unity-version $unity_version;
done & # <- use & to run the entire loop in the background
