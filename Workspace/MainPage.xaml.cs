using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;
using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace Workspace {
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage {
        SKMatrix _matrix = SKMatrix.CreateIdentity();
        readonly Dictionary<long, SKPoint> _touchDictionary = new Dictionary<long, SKPoint>();
        List<Tuple<SKPath, SKPaint>> _paths;
        readonly Random _random = new Random();

        public MainPage() {
            InitializeComponent();
        }

        void PopulatePaths() {
            _paths = new List<Tuple<SKPath, SKPaint>>();
            var numberOfColumns = 200;
            var numberOfRows = 500; 
            //100 000 in total
            int width = (int)(canvasView.CanvasSize.Width / numberOfColumns);
            int height = (int)(canvasView.CanvasSize.Height / numberOfRows);
            for (int i = 0; i < numberOfColumns; i++) {
                for (int j = 0; j < numberOfRows; j++) {
                    SKPath path = new SKPath();

                    int left = width * i + _random.Next(10);
                    int top = height * j + _random.Next(15);
                    int right = left + width + _random.Next(25);
                    int bottom = top + height + _random.Next(30);

                    path.AddRect(new SKRect(left, top, right, bottom));
                    _paths.Add(new Tuple<SKPath, SKPaint>(path,
                        new SKPaint { IsAntialias = true, IsStroke = true, StrokeWidth = 2, Color = new SKColor(0xff, 0, 0, (byte)_random.Next(255)) }));
                }
            }
        }

        void SKCanvasView_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e) {
            if (_paths == null) {
                PopulatePaths();
            }
            SKCanvas canvas = e.Surface.Canvas;

            canvas.Clear();
            canvas.SetMatrix(_matrix);
            
            foreach (var rect in _paths) {
                e.Surface.Canvas.DrawPath(rect.Item1, rect.Item2);
            }
        }

        void CanvasView_Touch(object sender, SKTouchEventArgs e) {
            SKPoint point = e.Location;
      
            switch (e.ActionType) {
                case SKTouchAction.Pressed:
                    if (!_touchDictionary.ContainsKey(e.Id)) {
                        _touchDictionary.Add(e.Id, point);
                    }

                    break;
                case SKTouchAction.Moved:
                    if (_touchDictionary.ContainsKey(e.Id)) {
                        if (_touchDictionary.Count == 1) {
                            SKPoint prevPoint = _touchDictionary[e.Id];
                            _matrix.TransX += point.X - prevPoint.X;
                            _matrix.TransY += point.Y - prevPoint.Y;
                            canvasView.InvalidateSurface();
                        }
                        else if (_touchDictionary.Count >= 2) {
                            long[] keys = new long[_touchDictionary.Count];
                            _touchDictionary.Keys.CopyTo(keys, 0);

                            int pivotIndex = (keys[0] == e.Id) ? 1 : 0;

                            SKPoint pivotPoint = _touchDictionary[keys[pivotIndex]];
                            SKPoint prevPoint = _touchDictionary[e.Id];
                            SKPoint newPoint = point;

                            SKPoint oldVector = prevPoint - pivotPoint;
                            SKPoint newVector = newPoint - pivotPoint;

                            SKMatrix touchMatrix = SKMatrix.MakeRotation(0, pivotPoint.X, pivotPoint.Y);

                            float magnitudeRatio = Magnitude(oldVector) / Magnitude(newVector);
                            oldVector.X = magnitudeRatio * newVector.X;
                            oldVector.Y = magnitudeRatio * newVector.Y;

                            float scale = Magnitude(newVector) / Magnitude(oldVector);

                            if (!float.IsNaN(scale) && !float.IsInfinity(scale)) {
                                SKMatrix.PostConcat(ref touchMatrix,
                                    SKMatrix.MakeScale(scale, scale, pivotPoint.X, pivotPoint.Y));

                                SKMatrix.PostConcat(ref _matrix, touchMatrix);
                                canvasView.InvalidateSurface();
                            }
                        }

                        _touchDictionary[e.Id] = point;
                    }
                    break;

                case SKTouchAction.Released:
                case SKTouchAction.Cancelled:
                    if (_touchDictionary.ContainsKey(e.Id)) {
                        _touchDictionary.Remove(e.Id);
                    }
                    break;

            }
            e.Handled = true;
        }

        float Magnitude(SKPoint point) {
            return (float)Math.Sqrt(Math.Pow(point.X, 2) + Math.Pow(point.Y, 2));
        }
    }
}
