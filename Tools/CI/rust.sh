# clone the latest rust repo
git clone https://github.com/Unity-Technologies/mps-common-multiplayer-backend.git
cd ./mps-common-multiplayer-backend/runtime

# Install rust
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y
export PATH="$HOME/.cargo/bin:$PATH"

# Build the echo server
cargo build --example ngo_echo_server

# Run the echo server in the background - this will reuse the artifacts from the build
cargo run --example ngo_echo_server -- --port $ECHO_SERVER_PORT &

# Build the standalone server
cargo build

# Run the standalone server on an infinite loop in the background
while :; do cargo run -- --metrics-port 5000 standalone --port $COMB_SERVER_PORT -t 10m done &
