using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace XamarinCanvas
{
	public class PolygonCanvasElement : CanvasElement
	{
		List<Cairo.PointD> verticies;

		public Cairo.Color Color { get; set; }

		public PolygonCanvasElement (double width, double height)
		{
			SetPreferedSize (width, height);
			Color = new Cairo.Color (0, 0, 0);
			verticies = new List<Cairo.PointD> ();
		}

		public void SetVerticies (IEnumerable<Cairo.PointD> verticies)
		{
			this.verticies.Clear ();
			this.verticies.AddRange (verticies);
			Normalize ();
		}

		void Normalize ()
		{
			var max = verticies.Aggregate ((agg, next) => agg = new Cairo.PointD (Math.Max (agg.X, next.X), Math.Max (agg.Y, next.Y)));
			var min = verticies.Aggregate ((agg, next) => agg = new Cairo.PointD (Math.Min (agg.X, next.X), Math.Min (agg.Y, next.Y)));

			verticies.ForEach (vert => {
				vert.X = (vert.X + min.Y) / max.Y;
				vert.Y = (vert.Y + min.Y) / max.Y;
			});
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			// polygons have 3 sides
			if (verticies.Count <= 2)
				return;

			var renderVerts = verticies.Select (v => new Cairo.PointD (v.X * Width, v.Y * Height)).ToList ();
			context.MoveTo (renderVerts.First ());
			renderVerts.ForEach (v => context.LineTo (v));
		}

		protected override void OnRender (Cairo.Context context)
		{
			OnLayoutOutline (context);
			context.Color = Color.MultiplyAlpha (Opacity);
			context.Fill ();

			base.OnRender (context);
		}
	}
	
}
