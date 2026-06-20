# Unity Hierarchy Decoration Tool

Add custom background images to the Unity Hierarchy window.

This editor tool allows you to decorate the Hierarchy with your own images while keeping the normal Unity workflow intact.

## Preview

![Preview](Editor/스크린샷 2026-06-21 052015.jpg)

## Features

* Custom image rendering in the Hierarchy window
* Adjustable image transparency
* Supports large image slicing
* Works with expanded and collapsed hierarchy items
* Lightweight Editor extension
* Unity 6 compatible

## Installation

1. Download `ApplyImageToInspector.cs`
2. Place the script inside an `Editor` folder

```text
Assets/
└── Editor/
    └── ApplyImageToInspector.cs
```

3. Open Unity

4. Open the tool from:

```text
Tools
└── Apply Image To Inspector
```

## Usage

1. Open the tool window.
2. Select a texture.
3. Adjust the transparency value.
4. Apply the image.
5. The selected image will be rendered behind the Hierarchy window.

## Notes

* Editor only.
* Uses Unity internal APIs through Reflection.
* Future Unity updates may require modifications if internal APIs change.
* Tested on Unity 6.

## Why?

Sometimes a little customization makes the editor more enjoyable to use.

This tool was created simply for fun and personalization.

## Author

**DoHan Kim**

Email: [aksia3@naver.com](mailto:aksia3@naver.com)

If you find a bug or have suggestions, feel free to contact me.
