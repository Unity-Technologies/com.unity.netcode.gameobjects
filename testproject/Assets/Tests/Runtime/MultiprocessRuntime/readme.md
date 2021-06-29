# Multiprocess testing

## Why
Multiprocess testing can be used for different use cases like integration tests (MLAPI + actual transport for example) or performance testing. Anything requiring a more realistic environment for testing.

TODO use my doc

## How it's done
### Multiple processes orchestration
todo
### CI
todo
### Client-server test coordination
todo
### Context based step execution
todo

## How to use it
Test players need to be built first to test locally. Integration with CI should do this automatically.

![](readme-ressources/Building-Player.png)

Then run the tests.

Performance tests should only be run from external processes (not from editor). This way the server code will run in a build, just as much as client code.

![](readme-ressources/Multiprocess.png)


# Future considerations
- Integrate with local MultiInstance tests?
- Have ExecuteStepInContext a game facing feature for sequencing client-server actions?