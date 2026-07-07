# Code documentation

Use this folder for any code snippets that only need to compile.

Any code snippets that you want to run tests on should be put in [Runtime tests](../../Runtime/Documentation/README.md).

To embed code in documentation, use the following tag

```md
[!code-cs[](../../Tests/Editor/DocumentationCodeSamples/<Path/To/Test>.cs#SomeRegionName)]
```

With the code formatted like this

```cs
namespace DocumentationCodeSamples
{
    internal MyTestClass
    {
        #region SomeRegionName
        // All the code in this region block will be embedded without indentation in the docs.
        #endregion

        [Test]
        public void TestOfDocumentationCode()
        {
            ...
        }
    }
}
```
