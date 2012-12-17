using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gtk;

using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace XamarinCanvas
{
	public class HBoxCanvasElement : GroupCanvasElement
	{
		protected override void OnSizeAllocated (double width, double height)
		{
			double x = 0;
			foreach (var child in Children) {
				child.X = x;
				child.Y = 0;
				child.SetSize (child.PreferedWidth, height);
				x += child.Width;
			}
		}

		protected override void OnChildPreferedSizeChanged (object sender, EventArgs e)
		{
			var width = Children.Sum (c => c.PreferedWidth);
			var height = Children.Sum (c => c.PreferedHeight);

			SetPreferedSize (width, height);
		}
	}
	
}
