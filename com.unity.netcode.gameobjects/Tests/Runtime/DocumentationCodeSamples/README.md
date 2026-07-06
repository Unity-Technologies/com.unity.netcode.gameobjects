# Code documentation

Use this folder for any code examples that we want to test to ensure the runtime functionality isn't broken

Any code snippets that are small enough that you only want to check that they compile should be in [Editor tests](../../Editor/Documentation/README.md).

To embed code in documentation, use the following tag

```md
[!code-cs[](../../Tests/Runtime/DocumentationCodeSamples/<Path/To/Test>.cs#SomeRegionName)]
```

With the code formatted like this

```cs
namespace DocumentationCodeSamples
{
    internal MyTestClass : NetcodeIntegrationTest
    {
        #region SomeRegionName
        // All the code in this region block will be embedded without indentation in the docs.
        #endregion

        protected override int NumberOfClients => 1;

        [UnityTest]
        public IEnumerator TestOfDocumentationCode()
        {
            ...
        }
    }
}
```
