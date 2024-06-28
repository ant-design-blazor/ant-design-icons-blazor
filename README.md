# AntDesign Icons for Blazor

## How it works?

Generate Icon components from official ant-design-icons repo with Source Generators.

## Usage

```razor
@AlertTwotone.RenderIcon(twoToneColor:["#52c41a", "#398439"])

<TagTwotone TwoToneColor="@(["red","yellow"])" />
```