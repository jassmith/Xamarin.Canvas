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
		ReadOnlyCollection<CanvasElement> roChildren;

		public override ReadOnlyCollection<CanvasElement> Children {
			get {
				return roChildren;
			}
		}

		public GroupCanvasElement ()
		{
			children = new List<CanvasElement> ();
			roChildren = children.AsReadOnly ();
		}

		public void Add (CanvasElement element)
		{
			children.Add (element);
			element.PreferedSizeChanged += OnChildPreferedSizeChanged;
			element.Parent = this;
			QueueDraw ();
		}

		protected virtual void OnChildPreferedSizeChanged (object sender, EventArgs e)
		{
			CanvasElement element = sender as CanvasElement;
			element.SetSize (element.PreferedWidth, element.PreferedHeight);
		}

		public void Remove (CanvasElement element)
		{
			if (children.Remove (element)) {
				element.Parent = null;
				element.PreferedSizeChanged -= OnChildPreferedSizeChanged;
				QueueDraw ();
			}
		}

		protected override void OnSizeAllocated (double width, double height)
		{
			foreach (var child in Children) {
				child.SetSize (child.PreferedWidth, child.PreferedHeight);
			}
		}
	}
	
}
