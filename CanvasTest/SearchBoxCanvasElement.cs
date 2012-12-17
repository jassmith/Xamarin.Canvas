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
	
	public class SearchBoxCanvasElement : GroupCanvasElement
	{
		LabelCanvasElement searchLabel;
		
		EntryCanvasElement searchEntry;
		EntryCanvasElement replaceEntry;
		
		ButtonCanvasElement searchButton;
		ButtonCanvasElement nextButton;
		ButtonCanvasElement prevButton;
		ButtonCanvasElement replaceButton;
		ButtonCanvasElement replaceAllButton;
		
		bool showReplace;
		public bool ShowReplace {
			get {
				return showReplace;
			}
			set {
				if (showReplace == value)
					return;
				showReplace = value;
				Layout (true, showReplace);
			}
		}
		
		public SearchBoxCanvasElement ()
		{
			showReplace = true;
			SetPreferedSize (700, 24);
			Build ();
		}
		
		void Build ()
		{
			BuildSearchButton ();
			
			searchEntry = new EntryCanvasElement ();
			searchEntry.SetPreEditLabel ("Type to search...");
			Add (searchEntry);
			
			replaceEntry = new EntryCanvasElement ();
			replaceEntry.SetPreEditLabel ("Replace with...");
			Add (replaceEntry);
			
			nextButton = new ButtonCanvasElement (new ImageCanvasElement (LoadPixbuf ("go-next-ltr.png")));
			prevButton = new ButtonCanvasElement (new ImageCanvasElement (LoadPixbuf ("go-previous-ltr.png")));
			PrepButton (nextButton);
			PrepButton (prevButton);
			Add (nextButton);
			Add (prevButton);
			
			replaceButton = new ButtonCanvasElement (new LabelCanvasElement ("Replace"));
			replaceAllButton = new ButtonCanvasElement (new LabelCanvasElement ("Replace All"));
			PrepButton (replaceButton);
			PrepButton (replaceAllButton);
			Add (replaceButton);
			Add (replaceAllButton);
		}
		
		void BuildSearchButton ()
		{
			GroupCanvasElement box = new HBoxCanvasElement ();
			
			var image = new ImageCanvasElement (LoadPixbuf ("searchbox-search-16.png"));
			image.WidthRequest = 24;
			image.XAlign = 0;
			box.Add (image);
			
			searchLabel = new LabelCanvasElement ("Search");
			searchLabel.XAlign = 0;
			searchLabel.YAlign = 0.5;
			box.Add (searchLabel);
			
			searchButton = new ButtonCanvasElement (box);
			searchButton.ClickEvent += (sender, e) => ShowReplace = !ShowReplace;
			PrepButton (searchButton);
			Add (searchButton);
		}
		
		protected override void OnChildPreferedSizeChanged (object sender, EventArgs e)
		{
			Layout (false, ShowReplace);
		}
		
		protected override void OnSizeAllocated (double width, double height)
		{
			Layout (false, ShowReplace);
		}
		
		void SetPosition (CanvasElement element, double x, double y, bool animate)
		{
			if (animate) {
				element.MoveTo (x, y, 400, Easing.CubicOut);
			} else {
				element.X = x;
				element.Y = y;
			}
		}
		
		void SizeChild (CanvasElement element, double width, double height, bool animate)
		{
			if (animate) {
				element.SizeTo (width, height, 400, Easing.CubicOut);
			} else {
				element.SetSize (width, height);
			}
		}
		
		void Layout (bool animate, bool showReplace)
		{
			double x = 0;
			double entryWidth;
			if (showReplace) {
				double nonEntryWidth = nextButton.PreferedWidth + prevButton.PreferedWidth + 
					replaceButton.PreferedWidth + replaceAllButton.PreferedWidth + searchButton.PreferedWidth;
				entryWidth = (int) ((Math.Max (2, Width - nonEntryWidth)) / 2);
			} else {
				double nonEntryWidth = nextButton.PreferedWidth + prevButton.PreferedWidth + searchButton.PreferedWidth;
				entryWidth = (int) ((Math.Max (2, Width - nonEntryWidth)));
			}
			
			SetPosition (searchButton, x, 0, animate);
			searchButton.SetSize (searchButton.PreferedWidth, PreferedHeight);
			x += searchButton.Width;
			
			SetPosition (searchEntry, x, 0, animate);
			SizeChild (searchEntry, entryWidth, PreferedHeight, animate);
			x += entryWidth;
			
			SetPosition (prevButton, x + 1, 0, animate);
			prevButton.SetSize (prevButton.PreferedWidth, PreferedHeight);
			x += prevButton.Width;
			
			SetPosition (nextButton, x, 0, animate);
			nextButton.SetSize (nextButton.PreferedWidth, PreferedHeight);
			x += nextButton.Width;
			
			SetPosition (replaceEntry, x, 0, animate);
			SizeChild (replaceEntry, entryWidth, PreferedHeight, animate);
			x += entryWidth;
			
			SetPosition (replaceButton, x + 1, 0, animate);
			replaceButton.SetSize (replaceButton.PreferedWidth, PreferedHeight);
			x += replaceButton.Width;
			
			SetPosition (replaceAllButton, x, 0, animate);
			replaceAllButton.SetSize (replaceAllButton.PreferedWidth + 2, PreferedHeight);
			x += replaceAllButton.Width;
			
			replaceEntry.InputTransparent = !showReplace;
			replaceButton.InputTransparent = !showReplace;
			replaceAllButton.InputTransparent = !showReplace;
		}
		
		void PrepButton (ButtonCanvasElement button)
		{
			button.Rounding = 0;
			button.InternalPadding = 6;
			button.CanFocus = false;
		}
		
		Gdk.Pixbuf LoadPixbuf (string name)
		{
			return Gdk.Pixbuf.LoadFromResource (name);
		}
		
		void ClipBackground (Cairo.Context context, Gdk.Rectangle region)
		{
			int rounding = (region.Height - 2) / 2;
			context.RoundedRectangle (region.X, region.Y + 2, region.Width, region.Height - 4, rounding);
			context.Clip ();
		}
		
		void DrawOutline (Cairo.Context context, Gdk.Rectangle region, float opacity)
		{
			int rounding = (region.Height - 2) / 2;
			context.RoundedRectangle (region.X + 0.5, region.Y + 1.5, region.Width - 1, region.Height - 2, rounding);
			using (var lg = new Cairo.LinearGradient (0, region.Y, 0, region.Bottom)) {
				lg.AddColorStop (0, CairoExtensions.ParseColor ("f2f2f2", opacity));
				lg.AddColorStop (1, CairoExtensions.ParseColor ("e7e7e7", opacity));
				context.Pattern = lg;
				context.LineWidth = 1;
				context.Stroke ();
			}
			
			context.RoundedRectangle (region.X + 0.5, region.Y + 0.5, region.Width - 1, region.Height - 2, rounding);
			using (var lg = new Cairo.LinearGradient (0, region.Y, 0, region.Bottom)) {
				lg.AddColorStop (0, CairoExtensions.ParseColor ("c7c7c7", opacity));
				lg.AddColorStop (1, CairoExtensions.ParseColor ("bcbcbc", opacity));
				context.Pattern = lg;
				context.LineWidth = 1;
				context.Stroke ();
			}
		}
		
		protected override void OnRender (Cairo.Context context)
		{
			OnLayoutOutline (context);
			context.Clip ();
			context.Color = new Cairo.Color (1, 1, 1, Opacity);
			context.Paint ();
			
			Gdk.Rectangle region = new Gdk.Rectangle (0, 0, (int)Width, (int)Height); 
			
			context.Save ();
			ClipBackground (context, region);
			base.OnRender (context);
			context.Restore ();
			DrawOutline (context, region, 1);
		}
		
		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.RoundedRectangle (0, 0, Width, Height, Height / 2);
		}
	}
	
}
