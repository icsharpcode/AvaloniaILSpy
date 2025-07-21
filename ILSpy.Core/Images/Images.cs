// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using Avalonia.Media.Imaging;
using Avalonia;
using System.Collections.Generic;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;

namespace ICSharpCode.ILSpy
{
    static class Images
	{
		static Bitmap LoadBitmap(string name)
		{
			Bitmap image = new Bitmap("Images/" + name + ".png");
			//image.Freeze();
			return image;
		}
		
		public static readonly Bitmap Breakpoint = LoadBitmap("Breakpoint");
		public static readonly Bitmap CurrentLine = LoadBitmap("CurrentLine");

		public static readonly Bitmap ViewCode = LoadBitmap("ViewCode");
		public static readonly Bitmap Save = LoadBitmap("SaveFile");
		public static readonly Bitmap OK = LoadBitmap("OK");

		public static readonly Bitmap Delete = LoadBitmap("Delete");
		public static readonly Bitmap Search = LoadBitmap("Search");

		public static readonly Bitmap Assembly = LoadBitmap("Assembly");
		public static readonly Bitmap AssemblyWarning = LoadBitmap("AssemblyWarning");
		public static readonly Bitmap AssemblyLoading = LoadBitmap("FindAssembly");

		public static readonly Bitmap Library = LoadBitmap("Library");
		public static readonly Bitmap Namespace = LoadBitmap("NameSpace");

		public static readonly Bitmap ReferenceFolderOpen = LoadBitmap("ReferenceFolder.Open");
		public static readonly Bitmap ReferenceFolderClosed = LoadBitmap("ReferenceFolder.Closed");

		public static readonly Bitmap SubTypes = LoadBitmap("SubTypes");
		public static readonly Bitmap SuperTypes = LoadBitmap("SuperTypes");

		public static readonly Bitmap FolderOpen = LoadBitmap("Folder.Open");
		public static readonly Bitmap FolderClosed = LoadBitmap("Folder.Closed");

		public static readonly Bitmap Resource = LoadBitmap("Resource");
		public static readonly Bitmap ResourceImage = LoadBitmap("ResourceImage");
		public static readonly Bitmap ResourceResourcesFile = LoadBitmap("ResourceResourcesFile");
		public static readonly Bitmap ResourceXml = LoadBitmap("ResourceXml");
		public static readonly Bitmap ResourceXsd = LoadBitmap("ResourceXsd");
		public static readonly Bitmap ResourceXslt = LoadBitmap("ResourceXslt");

		public static readonly Bitmap Class = LoadBitmap("Class");
		public static readonly Bitmap Struct = LoadBitmap("Struct");
		public static readonly Bitmap Interface = LoadBitmap("Interface");
		public static readonly Bitmap Delegate = LoadBitmap("Delegate");
		public static readonly Bitmap Enum = LoadBitmap("Enum");
		public static readonly Bitmap StaticClass = LoadBitmap("StaticClass");

		public static readonly Bitmap Field = LoadBitmap("Field");
		public static readonly Bitmap FieldReadOnly = LoadBitmap("FieldReadOnly");
		public static readonly Bitmap Literal = LoadBitmap("Literal");
		public static readonly Bitmap EnumValue = LoadBitmap("EnumValue");

		public static readonly Bitmap Method = LoadBitmap("Method");
		public static readonly Bitmap Constructor = LoadBitmap("Constructor");
		public static readonly Bitmap VirtualMethod = LoadBitmap("VirtualMethod");
		public static readonly Bitmap Operator = LoadBitmap("Operator");
		public static readonly Bitmap ExtensionMethod = LoadBitmap("ExtensionMethod");
		public static readonly Bitmap PInvokeMethod = LoadBitmap("PInvokeMethod");

		public static readonly Bitmap Property = LoadBitmap("Property");
		public static readonly Bitmap Indexer = LoadBitmap("Indexer");

		public static readonly Bitmap Event = LoadBitmap("Event");

		private static readonly Bitmap OverlayProtected = LoadBitmap("OverlayProtected");
		private static readonly Bitmap OverlayInternal = LoadBitmap("OverlayInternal");
		private static readonly Bitmap OverlayProtectedInternal = LoadBitmap("OverlayProtectedInternal");
		private static readonly Bitmap OverlayPrivate = LoadBitmap("OverlayPrivate");
		private static readonly Bitmap OverlayPrivateProtected = LoadBitmap("OverlayPrivateProtected");
		private static readonly Bitmap OverlayCompilerControlled = LoadBitmap("OverlayCompilerControlled");

		private static readonly Bitmap OverlayStatic = LoadBitmap("OverlayStatic");

		public static Bitmap LoadImage(object part, string icon)
		{
            Bitmap image;
            var assembly = part.GetType().Assembly;
			if (assembly == typeof(Images).Assembly) {
				image = new Bitmap(icon);
			} else {
				var name = assembly.GetName();
                var embededResourceStream = assembly.GetManifestResourceStream(icon);
                image = new Bitmap(embededResourceStream);
            }

			return image;
		}

		private static readonly TypeIconCache typeIconCache = new TypeIconCache();
		private static readonly MemberIconCache memberIconCache = new MemberIconCache();

		public static Bitmap GetIcon(TypeIcon icon, AccessOverlayIcon overlay, bool isStatic = false)
		{
			lock (typeIconCache)
				return typeIconCache.GetIcon(icon, overlay, isStatic);
		}

		public static Bitmap GetIcon(MemberIcon icon, AccessOverlayIcon overlay, bool isStatic)
		{
			lock (memberIconCache)
				return memberIconCache.GetIcon(icon, overlay, isStatic);
		}

		#region icon caches & overlay management

		private class TypeIconCache : IconCache<TypeIcon>
		{
			public TypeIconCache()
			{
				PreloadPublicIconToCache(TypeIcon.Class, Images.Class);
				PreloadPublicIconToCache(TypeIcon.Enum, Images.Enum);
				PreloadPublicIconToCache(TypeIcon.Struct, Images.Struct);
				PreloadPublicIconToCache(TypeIcon.Interface, Images.Interface);
				PreloadPublicIconToCache(TypeIcon.Delegate, Images.Delegate);
				PreloadPublicIconToCache(TypeIcon.StaticClass, Images.StaticClass);
			}

			protected override Bitmap GetBaseImage(TypeIcon icon)
			{
				Bitmap baseImage;
				switch (icon) {
					case TypeIcon.Class:
						baseImage = Images.Class;
						break;
					case TypeIcon.Enum:
						baseImage = Images.Enum;
						break;
					case TypeIcon.Struct:
						baseImage = Images.Struct;
						break;
					case TypeIcon.Interface:
						baseImage = Images.Interface;
						break;
					case TypeIcon.Delegate:
						baseImage = Images.Delegate;
						break;
					case TypeIcon.StaticClass:
						baseImage = Images.StaticClass;
						break;
					default:
                        throw new ArgumentOutOfRangeException(nameof(icon), $"TypeIcon.{icon} is not supported!");
                }

				return baseImage;
			}
		}

		private class MemberIconCache : IconCache<MemberIcon>
		{
			public MemberIconCache()
			{
				PreloadPublicIconToCache(MemberIcon.Field, Images.Field);
				PreloadPublicIconToCache(MemberIcon.FieldReadOnly, Images.FieldReadOnly);
				PreloadPublicIconToCache(MemberIcon.Literal, Images.Literal);
				PreloadPublicIconToCache(MemberIcon.EnumValue, Images.EnumValue);
				PreloadPublicIconToCache(MemberIcon.Property, Images.Property);
				PreloadPublicIconToCache(MemberIcon.Indexer, Images.Indexer);
				PreloadPublicIconToCache(MemberIcon.Method, Images.Method);
				PreloadPublicIconToCache(MemberIcon.Constructor, Images.Constructor);
				PreloadPublicIconToCache(MemberIcon.VirtualMethod, Images.VirtualMethod);
				PreloadPublicIconToCache(MemberIcon.Operator, Images.Operator);
				PreloadPublicIconToCache(MemberIcon.ExtensionMethod, Images.ExtensionMethod);
				PreloadPublicIconToCache(MemberIcon.PInvokeMethod, Images.PInvokeMethod);
				PreloadPublicIconToCache(MemberIcon.Event, Images.Event);
			}

			protected override Bitmap GetBaseImage(MemberIcon icon)
			{
				Bitmap baseImage;
				switch (icon) {
					case MemberIcon.Field:
						baseImage = Images.Field;
						break;
					case MemberIcon.FieldReadOnly:
						baseImage = Images.FieldReadOnly;
						break;
					case MemberIcon.Literal:
						baseImage = Images.Literal;
						break;
					case MemberIcon.EnumValue:
						baseImage = Images.Literal;
						break;
					case MemberIcon.Property:
						baseImage = Images.Property;
						break;
					case MemberIcon.Indexer:
						baseImage = Images.Indexer;
						break;
					case MemberIcon.Method:
						baseImage = Images.Method;
						break;
					case MemberIcon.Constructor:
						baseImage = Images.Constructor;
						break;
					case MemberIcon.VirtualMethod:
						baseImage = Images.VirtualMethod;
						break;
					case MemberIcon.Operator:
						baseImage = Images.Operator;
						break;
					case MemberIcon.ExtensionMethod:
						baseImage = Images.ExtensionMethod;
						break;
					case MemberIcon.PInvokeMethod:
						baseImage = Images.PInvokeMethod;
						break;
					case MemberIcon.Event:
						baseImage = Images.Event;
						break;
					default:
                        throw new ArgumentOutOfRangeException(nameof(icon), $"MemberIcon.{icon} is not supported!");
                }

				return baseImage;
			}
		}

        private class WbFb : IFramebufferPlatformSurface
        {
            WriteableBitmap _bitmap;
            public ILockedFramebuffer Lock() => _bitmap.Lock();

			// Avalonia v11 - https://github.com/AvaloniaUI/Avalonia/pull/11914
			public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);

			public WbFb(WriteableBitmap bitmap)
            {
                _bitmap = bitmap;
            }
        }

        private abstract class IconCache<T>
		{
			private readonly Dictionary<Tuple<T, AccessOverlayIcon, bool>, Bitmap> cache = new Dictionary<Tuple<T, AccessOverlayIcon, bool>, Bitmap>();

			protected void PreloadPublicIconToCache(T icon, Bitmap image)
			{
				var iconKey = new Tuple<T, AccessOverlayIcon, bool>(icon, AccessOverlayIcon.Public, false);
				cache.Add(iconKey, image);
			}

			public Bitmap GetIcon(T icon, AccessOverlayIcon overlay, bool isStatic)
			{
				var iconKey = new Tuple<T, AccessOverlayIcon, bool>(icon, overlay, isStatic);
				if (cache.ContainsKey(iconKey)) {
					return cache[iconKey];
				} else {
					Bitmap result = BuildMemberIcon(icon, overlay, isStatic);
					cache.Add(iconKey, result);
					return result;
				}
			}

			private Bitmap BuildMemberIcon(T icon, AccessOverlayIcon overlay, bool isStatic)
			{
				Bitmap baseImage = GetBaseImage(icon);
				Bitmap overlayImage = GetOverlayImage(overlay);

				return CreateOverlayImage(baseImage, overlayImage, isStatic);
			}

			protected abstract Bitmap GetBaseImage(T icon);

			private static Bitmap GetOverlayImage(AccessOverlayIcon overlay)
			{
				Bitmap overlayImage;
				switch (overlay) {
					case AccessOverlayIcon.Public:
						overlayImage = null;
						break;
					case AccessOverlayIcon.Protected:
						overlayImage = Images.OverlayProtected;
						break;
					case AccessOverlayIcon.Internal:
						overlayImage = Images.OverlayInternal;
						break;
					case AccessOverlayIcon.ProtectedInternal:
						overlayImage = Images.OverlayProtectedInternal;
						break;
					case AccessOverlayIcon.Private:
						overlayImage = Images.OverlayPrivate;
						break;
					case AccessOverlayIcon.PrivateProtected:
						overlayImage = Images.OverlayPrivateProtected;
						break;
					case AccessOverlayIcon.CompilerControlled:
						overlayImage = Images.OverlayCompilerControlled;
						break;
					default:
                        throw new ArgumentOutOfRangeException(nameof(overlay), $"AccessOverlayIcon.{overlay} is not supported!");
                }
				return overlayImage;
			}

			private static readonly Rect iconRect = new Rect(0, 0, 16, 16);

            private static Bitmap CreateOverlayImage(Bitmap baseImage, Bitmap overlay, bool isStatic)
			{
                var image = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);

				// Avalonia 0.10 - https://github.com/AvaloniaUI/Avalonia/pull/11557
				using (var rt = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().CreateRenderTarget(new[] { new WbFb(image)})) {

                    using (var ctx = rt.CreateDrawingContext(null)) {

                        ctx.DrawBitmap(baseImage.PlatformImpl, 1.0, iconRect, iconRect);

                        if (overlay != null) {
                            ctx.DrawBitmap(overlay.PlatformImpl, 1.0, iconRect, iconRect);
                        }

                        if (isStatic) {
                            ctx.DrawBitmap(Images.OverlayStatic.PlatformImpl, 1.0, iconRect, iconRect);
                        }

                    }

                }

                // TODO: image.Freeze()
                return image;
            }
        }

		#endregion
	}
}
