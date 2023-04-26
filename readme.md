# What
This application will patch original Barotrauma resources by defined xml replacements.

# How
Replacements file format is loosely based on [Barotrauma Mod Generator](https://barotrauma-mod-generator-docs.readthedocs.io/en/latest/usage.html).

### Diff and DiffCollection

Replacements are grouped in `Diff` node. One `Diff` node can specify single content file.
`Diff` attributes:
 - [required] `file`  - relative from root directory path to patched file
 - [optional] `order` - sequential order of applying multiple `Diff` patches to same content file

To patch multiple content files with single replacement file, group multiple `Diff` nodes inside `DiffCollection` node.

### replace

Atomic replacement based on XPath. Attributes:

- [required] `sel` - XPath selector to XML Element within file that should be patched
- [optional] `asNode="true"` - if set, whole selected node would be replaced, not just its inner XML

Replace inner XML would be used as replace value. Note that you can replace singular attributes as well as content nodes inner xml.

### add

Adds new element. Attributes:

- [required] `sel` - XPath selector to parent for the new element
- [optional] `after` - relative to parent XPath selector that specifies previous sibling

### text-replace

Replaces raw text. If applied to XML file, may result in invalid XML. Attributes:

- [required] `sel` - text to search
- [optional] `ignore-case="true"` - if set, case would be ignored (not ignored by default)
