# MessageChunker Class
 

Helper class to chunk messages


## Inheritance Hierarchy
<a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">System.Object</a><br />&nbsp;&nbsp;MLAPI.NetworkingManagerComponents.MessageChunker<br />
**Namespace:**&nbsp;<a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents</a><br />**Assembly:**&nbsp;MLAPI (in MLAPI.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
public static class MessageChunker
```

<br />
The MessageChunker type exposes the following members.


## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_MLAPI_NetworkingManagerComponents_MessageChunker_GetChunkedMessage">GetChunkedMessage</a></td><td>
Chunks a large byte array to smaller chunks</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_MLAPI_NetworkingManagerComponents_MessageChunker_GetMessageOrdered">GetMessageOrdered</a></td><td>
Converts a list of chunks back into the original buffer, this requires the list to be in correct order and properly verified</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_MLAPI_NetworkingManagerComponents_MessageChunker_GetMessageUnordered">GetMessageUnordered</a></td><td>
Converts a list of chunks back into the original buffer, this does not require the list to be in correct order and properly verified</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_MLAPI_NetworkingManagerComponents_MessageChunker_HasDuplicates">HasDuplicates</a></td><td>
Checks if a list of chunks have any duplicates inside of it</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_MLAPI_NetworkingManagerComponents_MessageChunker_HasMissingParts">HasMissingParts</a></td><td>
Checks if a list of chunks has missing parts</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_MLAPI_NetworkingManagerComponents_MessageChunker_IsOrdered">IsOrdered</a></td><td>
Checks if a list of chunks is in correct order</td></tr></table>&nbsp;
<a href="#messagechunker-class">Back to Top</a>

## See Also


#### Reference
<a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents Namespace</a><br />