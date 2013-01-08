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
	public class BreadcrumbElement : GroupCanvasElement
	{
		public BreadcrumbElement ()
		{

		}

		protected override void OnSizeAllocated (double width, double height)
		{

		}

		protected override void OnChildPreferedSizeChanged (object sender, EventArgs e)
		{

		}
	}

	public class BreadcrumbEntryElement : GroupCanvasElement
	{
		ImageCanvasElement image;
		LabelCanvasElement label;

		public int InternalPadding { get; set; }

		public PathEntry Path { get; set; }

		public BreadcrumbEntryElement (PathEntry path)
		{
			InternalPadding = 5;
			Path = path;
			Build ();
			Opacity = 0.5;
		}

		void Build ()
		{
			if (Path.Icon != null) {
				image = new ImageCanvasElement (Path.Icon);
				Add (image);
			}

			label = new LabelCanvasElement ();
			label.Markup = Path.Markup;
			Add (label);
		}

		protected override void OnMouseIn ()
		{
			FadeTo (1.0);
		}

		protected override void OnMouseOut ()
		{
			FadeTo (0.5);
		}

		protected override void OnSizeAllocated (double width, double height)
		{
			Layout ();
		}

		protected override void OnChildPreferedSizeChanged (object sender, EventArgs e)
		{
			var width = Children.Sum (c => c.PreferedWidth);
			var height = 30;

			// Add in padding
			width += InternalPadding * (1 + Children.Count);

			SetPreferedSize (width, height);
		}

		void Layout ()
		{
			double x = InternalPadding;
			foreach (var child in Children) {
				child.SetSize (child.PreferedWidth, Height);
				child.X = X;
				child.Y = 0;

				x += child.Width + InternalPadding;
			}
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.Rectangle (0, 0, Width, Height);
		}

		protected override void OnRender (Cairo.Context context)
		{
			context.Rectangle (0.5, 0.5, Width - 1, Height - 1);
			context.LineWidth = 1;
			context.Color = new Cairo.Color (0, 0, 0, Math.Max (0, Opacity - 0.5));
			context.Stroke ();
			base.OnRender (context);
		}
	}

	public class SearchBoxButton : ButtonCanvasElement
	{
		LabelCanvasElement searchLabel;
		GroupCanvasElement box;
		ImageCanvasElement image;

		public string Text {
			get { return searchLabel.Text; }
			set { searchLabel.Text = value; }
		}

		public override Gdk.CursorType Cursor {
			get {
				return IconOnly ? Gdk.CursorType.Hand1 : Gdk.CursorType.XCursor;
			}
		}

		bool iconOnly;
		public bool IconOnly {
			get {
				return iconOnly;
			}
			set {
				if (iconOnly == value)
					return;
				iconOnly = value;
				FadeTo (value ? 0 : 1);

				if (!value)
					ZoomImageAnimation ();
			}
		}

		public SearchBoxButton ()
		{
			Build ();
		}

		void ZoomImageAnimation ()
		{
			image.ScaleTo (1.15, 150, Easing.SinOut)
				.ContinueWith (aborted => image.ScaleTo (1, 150, Easing.SinIn));
		}

		void Build ()
		{
			box = new HBoxCanvasElement ();
			
			image = new ImageCanvasElement (Gdk.Pixbuf.LoadFromResource ("searchbox-search-16.png"));
			image.WidthRequest = 24;
			image.XAlign = 0;
			image.NoChainOpacity = true;
			box.Add (image);
			
			searchLabel = new LabelCanvasElement ("Search");
			searchLabel.XAlign = 0;
			searchLabel.YAlign = 0.5;
			box.Add (searchLabel);
			
			var upDownArrows = new CanvasElement ();
			upDownArrows.WidthRequest = 12;
			upDownArrows.HeightRequest = 24;
			
			upDownArrows.RenderEvent += (sender, e) => {
				var context = e.Context;
				double centerX = upDownArrows.Width - 3;
				double centerY = upDownArrows.Height / 2;
				context.MoveTo (centerX, centerY - 6);
				context.LineTo (centerX - 3, centerY - 2);
				context.LineTo (centerX + 3, centerY - 2);
				context.ClosePath ();
				
				context.MoveTo (centerX, centerY + 6);
				context.LineTo (centerX - 3, centerY + 2);
				context.LineTo (centerX + 3, centerY + 2);
				context.ClosePath ();
				
				context.Color = new Cairo.Color (0, 0, 0, 0.35 * upDownArrows.Opacity);
				context.Fill ();
			};

			image.SizeChanged += (object sender, EventArgs e) => {
				image.AnchorX = 8;
				image.AnchorY = image.Height / 2;
			};
			
			box.Add (upDownArrows);

			SetChild (box);
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			if (IconOnly) {
				context.Transform (box.Transform);
				context.Transform (image.Transform);
				image.LayoutOutline (context);
			} else {
				base.OnLayoutOutline (context);
			}
		}
	}


	public class SearchBoxCanvasElement : GroupCanvasElement
	{
		enum SearchMode {
			Search,
			Replace,
			GoToLine,
			NavigateTo,
		}
		
		EntryCanvasElement searchEntry;
		EntryCanvasElement replaceEntry;
		
		SearchBoxButton searchButton;
		ButtonCanvasElement nextButton;
		ButtonCanvasElement prevButton;
		ButtonCanvasElement replaceButton;
		ButtonCanvasElement replaceAllButton;

		GroupCanvasElement entryGroup;
		HBoxCanvasElement breadcrumbGroup;

		bool showReplace;
		bool ShowReplace {
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

		bool entryMode;
		bool EntryMode {
			get {
				return entryMode;
			}
			set {
				if (entryMode == value)
					return;
				entryMode = value;
				entryGroup.FadeTo (value ? 1 : 0);
				entryGroup.InputTransparent = !value;
				searchButton.IconOnly = !value;
			}
		}
		
		public SearchBoxCanvasElement ()
		{
			showReplace = false;
			SetPreferedSize (700, 24);
			BuildEntryGroup ();
			BuildSearchButton ();

			nextButton.ClickEvent += (sender, e) => {
				EntryMode = !EntryMode;
			};

			entryMode = false;
			entryGroup.Opacity = 0;
			entryGroup.InputTransparent = true;
			searchButton.IconOnly = true;
		}
		
		void BuildEntryGroup ()
		{
			entryGroup = new GroupCanvasElement ();

			searchEntry = new EntryCanvasElement ();
			searchEntry.SetPreEditLabel ("Type to search...");
			entryGroup.Add (searchEntry);

			replaceEntry = new EntryCanvasElement ();
			replaceEntry.SetPreEditLabel ("Replace with...");
			entryGroup.Add (replaceEntry);
			
			nextButton = new ButtonCanvasElement (new ImageCanvasElement (Gdk.Pixbuf.LoadFromResource ("go-next-ltr.png")));
			prevButton = new ButtonCanvasElement (new ImageCanvasElement (Gdk.Pixbuf.LoadFromResource ("go-previous-ltr.png")));
			PrepButton (nextButton);
			PrepButton (prevButton);
			entryGroup.Add (nextButton);
			entryGroup.Add (prevButton);
			
			replaceButton = new ButtonCanvasElement (new LabelCanvasElement ("Replace"));
			replaceAllButton = new ButtonCanvasElement (new LabelCanvasElement ("Replace All"));
			PrepButton (replaceButton);
			PrepButton (replaceAllButton);
			entryGroup.Add (replaceButton);
			entryGroup.Add (replaceAllButton);

			Add (entryGroup);
		}

		void BuildBreadcrumbGroup ()
		{

		}
		
		void BuildSearchButton ()
		{
			searchButton = new SearchBoxButton ();
			searchButton.MenuItems = new List<MenuEntry> (new []{ 
				new MenuEntry ("Search", SearchMode.Search), 
				new MenuEntry ("Replace", SearchMode.Replace),
				new MenuEntry ("Go To Line", SearchMode.GoToLine),
				new MenuEntry ("Navigate To", SearchMode.NavigateTo)
			});
			
			searchButton.ButtonPressEvent += (object sender, ButtonEventArgs e) => {
				if (EntryMode)
					searchButton.ShowMenu ((int)(e.XRoot - e.X), (int)(e.YRoot - e.Y - 5), e.Button);
				else
					EntryMode = true;
			};
			searchButton.MenuItemActivatedEvent += HandleSearchButtonMenuItemActivated;

			PrepButton (searchButton);
			Add (searchButton);
		}

		void HandleSearchButtonMenuItemActivated (object sender, MenuItemActivatedArgs e)
		{
			SearchMode mode = (SearchMode)e.Entry.Data;
			switch (mode) {
			case SearchMode.Search:
				ShowReplace = false;
				searchButton.Text = "Search";
				searchEntry.SetPreEditLabel ("Type to search...");
				break;
			case SearchMode.Replace:
				ShowReplace = true;
				searchButton.Text = "Replace";
				searchEntry.SetPreEditLabel ("Type to search...");
				break;
			case SearchMode.GoToLine:
				searchButton.Text = "Go To Line";
				searchEntry.SetPreEditLabel ("Go to line number...");
				ShowReplace = false;
				break;
			case SearchMode.NavigateTo:
				searchButton.Text = "Navigate To";
				searchEntry.SetPreEditLabel ("Find file or class...");
				ShowReplace = false;
				break;
			}
		}
		
		protected override void OnChildPreferedSizeChanged (object sender, EventArgs e)
		{
			Layout (true, ShowReplace);
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
		
		void ClipBackground (Cairo.Context context, Gdk.Rectangle region)
		{
			int rounding = (region.Height - 2) / 2;
			context.RoundedRectangle (region.X, region.Y + 2, region.Width, region.Height - 4, rounding);
			context.Clip ();
		}
		
		void DrawOutline (Cairo.Context context, Gdk.Rectangle region, double opacity)
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
			context.Color = new Cairo.Color (1, 1, 1, Opacity * entryGroup.Opacity);
			context.Paint ();
			
			Gdk.Rectangle region = new Gdk.Rectangle (0, 0, (int)Width, (int)Height); 
			
			context.Save ();
			ClipBackground (context, region);
			base.OnRender (context);
			context.Restore ();
			DrawOutline (context, region, Opacity * entryGroup.Opacity);
		}
		
		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.RoundedRectangle (0, 0, Width, Height, Height / 2);
		}
	}
	
}
