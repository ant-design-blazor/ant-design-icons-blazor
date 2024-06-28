# AntDesign Icons for Blazor

## How it works?

Generate Icon components from official ant-design-icons repo with Source Generators.

You can add all icons from https://ant.design/components/icon

## Usage

- Install package
  ```bash
  $ dotnet add package AntDesign
  ```

- Add `@using AntDesign.Icons` to your `_Imports.razor` file

- Use `@AlertTwotone.RenderIcon()` or `<AlertTwotone />` to render an icon.


    ```razor
    @AlertTwotone.RenderIcon(twoToneColor:["#52c41a", "#398439"])

    <TagTwotone TwoToneColor="@(["red","yellow"])" />
    ```
