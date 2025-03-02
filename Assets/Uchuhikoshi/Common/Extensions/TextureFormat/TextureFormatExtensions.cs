using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Uchuhikoshi
{
	public static class TextureFormatExtensions
	{
		public static bool IsHDRFormat(this TextureFormat self)
		{
			switch (self)
			{
				case TextureFormat.RGBAFloat:
				case TextureFormat.RGBAHalf:
				case TextureFormat.RGB9e5Float:
				case TextureFormat.BC6H:
					return true;
			}
			return false;
		}
	}
}