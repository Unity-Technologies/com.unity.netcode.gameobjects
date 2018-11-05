---
title: Home
permalink: /wiki/home/
redirect_from: /wiki/index.html
redirect_from: /wiki/index.md
redirect_from: /wiki/getting-started.md
redirect_from: /wiki/
---

Welcome to the MLAPI Community Wiki!

This is where you will find all the documentation for the MLAPI.




### Wiki Editing Guide
If you find anything you want to provide to the wiki, please do so. 
This wiki is built by the community, for the community. Below you can read
instructions on how to edit the wiki.

#### Changing Information
In the ``_docs`` folder is where the Markdown wiki files are located. To update information, update the content and submit a pullrequest.
#### Creating Pages
To create a page. Start out by creating the Markdown file in the ``_docs`` folder in the appropriate subfolder.
Secondly, add the file to the index by adding the following to the top of the file:
```yml
---
title: <Title>
permalink: /wiki/<Name>/
---
```

##### Example
Files should follow the following convention ``my-long-topic-name.md``
That would make the header:
```yml
---
title: My Long Topic Name
permalink: /wiki/my-long-topic-name/
---
```


The last step is to add it to the sidebar. This can be done by editing the ``docs.yml`` in the ``_data`` folder.
Simply add the part of the permalink after ``wiki``. The above examples entry in the ``docs.yml`` file would look like this


```yml
- title: Title Header Is Here
  docs:
  - my-long-topic-name
```

### Licence
Documentation on the wiki is licenced under the follwing licence
```
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <http://unlicense.org>
```