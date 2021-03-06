﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Media;

namespace ICSharpCode.Profiler.Controls
{
	public class PercentBar : FrameworkElement
	{
		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);
			
			if (RenderSize.Height > 0 && RenderSize.Width > 0) {
				drawingContext.DrawRectangle(new LinearGradientBrush(Colors.Red, Colors.Orange, 0), new Pen(Brushes.Black, 1), new Rect(new Point(0, 2), new Size(RenderSize.Width * Value, RenderSize.Height - 4)));
			}
		}
		
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register("Value", typeof(double), typeof(PercentBar),
			                            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

		
		public double Value
		{
			get { return (double)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}
	}
}
