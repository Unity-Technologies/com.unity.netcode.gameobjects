---
title: Boxing Systems
permalink: /wiki/boxing-systems/
---

While the MLAPI proudly puts itself forward as a performance API, one might get suprised over the amount of operations that are boxing and the amount of reflection used in the MLAPI. This page aims to explain where boxing takes place and why we justify it. **This is an advanced article for performance freaks.**


### Where
The first question is, where do we box or use reflection.

##### Convenience RPCs
Convenience RPCs box all their parameters and uses reflection to invoke the target method (lookup is only done once).
##### Custom Serialization Handlers
All custom serialization handlers are exposed by the API as generics, but behind the scenes the values get boxed. However they are never called by reflection.
##### Default NetworkedVar Implementations
All default NetworkedVar implementations box their values when writing and reading, no reflection is used however.


### Why
The second question is why we box and use reflection. Before that it's important to note that all of the above have alternatives that doesnt utilize reflection or boxing. Performance RPC's dont box and dont use reflection. Custom NetworkedVar containers doesnt box or use reflection. Back to the convenience API, lets start with boxing.

To use fast collections, we need compile time known types. The only way around this is to use a weaver. Onto the next question, why dont we weave the code?

The answer is fairly simple, it's messy and the advantage is minimal.

#### Performance
At the time of writing, the MLAPI is much more feature complete than competitors, offering a much more complex serialization pipeline with encryption, authentication, targeting and more. Despite this, the MLAPI has been optimized to run blazing fast. Running 1 milion RPCs and comparing the results with the currently largest competitors Mirror and UNET which both utilize weavers, the MLAPI is always more than 10% faster when using its convenience RPCs, and the performance RPCs are unmatched reaching about 30% faster than both the other libraries.

#### Clarity
Currently, weavers are dirty. They all require knowledge of IL and are very unclear to develop. Both libraries that today utilize a Weaver have a dirty, 1000+ line weaver codebase that only a few people know how to even edit, it's a pain to maintain. It also puts a high barrier of entry for contributing and maintaining in the future, makes it harder to debug etc.


### Conclusion
The MLAPI utilizes boxing and reflection where it will have minimal impact while still being invisible to the user. The MLAPI is all about options. If you need to get your code to run faster, you can use the performance alternatives. You will probably never notice the effects of the value boxing or reflection invocation.