# Avalonia v11 Upgrade

[Avalonia v11 Upgrade](https://docs.avaloniaui.net/docs/next/stay-up-to-date/upgrade-from-0.10)

## TO DO

* AvaloniaLocator has been removed
  * https://docs.avaloniaui.net/docs/next/stay-up-to-date/upgrade-from-0.10#locator
  * https://github.com/AvaloniaUI/Avalonia/pull/11557
* `protected override ... CreateItemContainerGenerator()`
  * This is not overridable. [Marked as obsolete](https://github.com/AvaloniaUI/Avalonia/blob/904b8ae287ac97968a75af64f1814b55f95b40b0/src/Avalonia.Controls/ItemsControl.cs#L635)
  * `private protected virtual ItemContainerGenerator CreateItemContainerGenerator()`
* `IItemContainerGenerator` -> `ItemContainerGenerator`
* `ItemContainerEventArgs` has been **deprecated**
  * [PR #9677](https://github.com/AvaloniaUI/Avalonia/pull/9677)
  * [Commit 1101f28 - Refactored ItemContainerGenerator](https://github.com/AvaloniaUI/Avalonia/commit/1101f28dd7a6460764873b6cc2e9ce4da293eb61)
    * `override void OnContainerMaterialized()` has been replaced by `internal override void PrepareContainerForItemOverride(...)`
* `MainWindow.HandleWindowStateChanged` has been **deprecated** in v11
  * NOTE: Undocumented
  * https://github.com/AvaloniaUI/Avalonia/discussions/11593
  * Use `protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)`

## Stretch TO-DO

* Rename `.xaml` to `.axaml`

## Updated

* `IBitmap` -> `Bitmap`
* `FocusManager`
* `WbFb : IFramebufferPlatformSurface` now implements `CreateFramebufferRenderTarget()` - https://github.com/AvaloniaUI/Avalonia/pull/11914

### Focus Manager

`OpenListDialog.xaml.cs`

```cs
// Avalonia v0.10
//// TemplateApplied += (sender, e) => Application.Current.FocusManager.Focus(listView);

TemplateApplied += (sender, e) => {
  // Avalonia v11
  var focusManager = TopLevel.GetTopLevel(listView).FocusManager;
  focusManager.GetFocusedElement().Focus();
};
```
