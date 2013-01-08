using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gtk;

using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

using Microsoft.Xna.Framework;

using FarseerPhysics.Dynamics;
using FarseerPhysics.Collision.Shapes;

namespace XamarinCanvas
{
	public class PhysicsBodyCanvasElement : CanvasElement
	{
		Body Body { get; set; }

		public PhysicsBodyCanvasElement (Body body)
		{
			Body = body;
		}

		void LayoutPolygon (Cairo.Context context, PolygonShape shape)
		{
			var verticies = shape.Vertices.Select (v => new Cairo.PointD (v.X * 64, v.Y * 64)).ToList ();
			context.MoveTo (verticies.First ());
			verticies.ForEach (v => context.LineTo (v));
			context.ClosePath ();


		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			if (Body.FixtureList == null)
				return;
			foreach (var fixture in Body.FixtureList) {
				switch (fixture.ShapeType) {
				case ShapeType.Circle:
					break;
				case ShapeType.Edge:

					break;
				case ShapeType.Loop:

					break;
				case ShapeType.Polygon:
					LayoutPolygon (context, fixture.Shape as PolygonShape);
					break;
				case ShapeType.TypeCount:

					break;
				case ShapeType.Unknown:

					break;
				}
			}
		}

		protected override void OnRender (Cairo.Context context)
		{
			if (Body.FixtureList == null)
				return;
			OnLayoutOutline (context);
			context.Color = new Cairo.Color (0, 0, 0, Opacity);
			context.Fill ();
		}
	}

	public class PhysicsCanvasElement : CanvasElement
	{
		World world;
		Body ground;
		List<Tuple<CanvasElement, Body>> bodies;
		System.Diagnostics.Stopwatch sw;

		public override ReadOnlyCollection<CanvasElement> Children {
			get {
				return bodies.Select (tuple => tuple.Item1).ToList ().AsReadOnly ();
			}
		}

		public PhysicsCanvasElement ()
		{
			SetPreferedSize (800, 800);
			sw = new System.Diagnostics.Stopwatch ();
			sw.Start ();


			GLib.Timeout.Add (10, () => {
				QueueDraw ();
				return true;
			});

			bodies = new List<Tuple<CanvasElement, Body>> ();
			BuildWorld ();
		}

		void AddBox (Vector2 position)
		{
			Body rectangle = FarseerPhysics.Factories.BodyFactory.CreateRectangle (world, 1, 1, 1, position);
			rectangle.BodyType = BodyType.Dynamic;
			rectangle.Restitution = 0.9f;
			rectangle.Friction = 0.5f;

			if (bodies.Count > 100) {
				var first = bodies.First ();
				world.RemoveBody (first.Item2);
			}
		}

		void BuildWorld ()
		{
			world = new World (new Vector2 (0, 9.82f));

			ground = FarseerPhysics.Factories.BodyFactory.CreateRectangle (world, 100, 1, 1, new Vector2 (1, 832 / 64f));
			ground.IsStatic = true;
			ground.Restitution = 0.3f;
			ground.Friction = 0.5f;

			GLib.Timeout.Add (200, () => {
				Random r = new Random ();
				AddBox (new Vector2 (2.5f + (float)r.NextDouble () * 8, -1));

				return true;
			});

			GLib.Timeout.Add (250, () => {
				FarseerPhysics.Common.PolygonManipulation.CuttingTools.Cut (world, new Vector2 (0, 1), new Vector2 (20, 5), 0.01f);
				return true;
			});

			world.BodyAdded += body => {
				if (body == ground)
					return;
				var element = new PhysicsBodyCanvasElement (body);
				bodies.Add (new Tuple<CanvasElement, Body> (element, body));
			};

			world.BodyRemoved += body => {
				bodies.RemoveAll (tup => tup.Item2 == body);
			};
		}

		void Step ()
		{
			sw.Stop ();
			var ms = sw.ElapsedMilliseconds;
			sw.Reset ();
			sw.Start ();
			
			world.Step (ms * 0.001f);
		}

		void UpdateChild (CanvasElement element, Body body)
		{
			element.X = body.Position.X * 64;
			element.Y = body.Position.Y * 64;
			element.X -= element.Width / 2;
			element.Y -= element.Height / 2;
			element.Rotation = body.Rotation;
		}

		protected override void OnRender (Cairo.Context context)
		{
			Step ();

			List<Tuple<CanvasElement, Body>> remove = new List<Tuple<CanvasElement, Body>> ();
			foreach (var child in bodies) {
				UpdateChild (child.Item1, child.Item2);

				if (child.Item1.X < -100 || child.Item1.X > 900) {
					remove.Add (child);
				}
			}

			foreach (var child in remove) {
				world.RemoveBody (child.Item2);
			}
			bodies.RemoveAll (tup => remove.Contains (tup));

			base.OnRender (context);
		}
	}
	
	class MainClass
	{

		public static void Main (string[] args)
		{
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.Resize (800, 800);
			
			Canvas canvas = new Canvas ();

			canvas.Realized += (sender, e) => {
//				SearchBoxCanvasElement searchBox = new SearchBoxCanvasElement ();
//				searchBox.WidthRequest = 790;
//				searchBox.X = 5;
//				searchBox.Y = 3;
//				canvas.AddElement (searchBox);

				PhysicsCanvasElement physics = new PhysicsCanvasElement ();
				physics.WidthRequest = 800;
				physics.HeightRequest = 800;
				canvas.AddElement (physics);
			};
			
			win.Add (canvas);

			win.ShowAll ();
			Application.Run ();
		}
	}
}
