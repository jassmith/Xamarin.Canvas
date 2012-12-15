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
	public class GroupCanvasElement : CanvasElement
	{
		List<CanvasElement> children;

		public override IEnumerable<CanvasElement> Children {
			get {
				return children.AsEnumerable ();
			}
		}

		public GroupCanvasElement ()
		{
			InputTransparent = true;
			children = new List<CanvasElement> ();
		}

		public void Add (CanvasElement element)
		{
			children.Add (element);
			element.Parent = this;
			QueueDraw ();
		}

		public void Remove (CanvasElement element)
		{
			if (children.Remove (element)) {
				element.Parent = null;
				QueueDraw ();
			}
		}
	}
	
}
