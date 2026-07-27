# Testing against the CMB Service

The CMB Service is a tool that is external to our repository. The tool is inside the `runtime` folder of the [CMB service](https://github.com/Unity-Technologies/unity-player-services/tree/main/services/common-multiplayer-backend) in the Unity Player Services monorepo.

Due to this, there is some more setup needed when running tests against the CMB Service.

## Configuration

The tests are automatically configured to run any `HostOrServer.DAHost` or `NetworkTopologyTypes.DistributedAuthority` test against the CMB server when either the `USE_CMB_SERVICE` scripting define is set, or when an environment variable is passed in with `USE_CMB_SERVICE=true`. When set, all non-distributed authority compatible tests will be ignored.

By default, the tests are configured to look for the service on the local machine (`localhost`/`http://127.0.0.1`) at port `7789`.

## Running against the service

First, ensure the `USE_CMB_SERVICE` scripting define or environment variable is set.

### Download the service

1. Go to the [CMB Runtime Build workflow](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/actions/workflows/cmb-runtime-build.yaml) on this repo's Actions page.
2. Trigger a new run with **Run workflow**
  a. optionally pass a unity-player-services branch, tag, or SHA (default will build `main`).
3. Wait for the action to finish running.

Inside the **Artifacts** section at the bottom of the page of the finished run you will see three pre-built `comb-server` binaries. Download the `comb-server-<platform>-latest` binary that matches your computer architecture.

### Run the service locally

Next we'll run the binary on the command line.

> [!NOTE]
> If you're running on macOS, you'll have to add execution privileges to the binary before running it.

```bash
xattr -c /path/to/comb-server
```

To run the service, run the following command:

```bash
/path/to/download/comb-server -l info --metrics-port 5000 standalone --port 7789 -t 60m
```

Note that we have set the port to `7789` to match where the tests will be looking.

After each test, all connected clients will disconnect from the service. The service will automatically shut down when that happens. When running multiple tests in a row, it can be more convenient to run the service in a loop:

```bash
while :; do /path/to/download/file -l info --metrics-port 5000 standalone --port 7789 -t 60m; done
```

### Run the tests

With `USE_CMB_SERVICE` set, everything should be configured so that running any distributed authority test in the editor should run against the service we have running on the command line. Try running a test to validate that information is logged in the command line.

## Further configuration

The following environment variables allow for further configuration of the setup.

`CMB_SERVICE_PORT` defines the port where the tests will try to connect to the service (defaults to `7789`).

`NGO_HOST` defines the http address where the tests will try to connect to the service (defaults to `127.0.0.1`).

## Running on CI (Yamato)

The CMB tests can also be run from Yamato. The jobs are defined in [`.yamato/cmb-service-standalone-tests.yml`](../../../.yamato/cmb-service-standalone-tests.yml) and appear in Yamato as `CMB Service Test - NGO <project> - [<platform>, <editor>, <backend>]`. The job can be triggered manually from any branch meaning it can be easier to run the CMB tests from Yamato rather than set them up locally. The test uses [`run_cmb_service.sh`](./run_cmb_service.sh) to setup and run the CMB service.
