// The MIT License(MIT)
//
// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.Motion;
using LiveChartsCore.Painting;

namespace LiveChartsCore;

/// <summary>
/// Defines a scatter series.
/// </summary>
/// <typeparam name="TModel">The type of the model.</typeparam>
/// <typeparam name="TVisual">The type of the visual.</typeparam>
/// <typeparam name="TLabel">The type of the label.</typeparam>
/// <seealso cref="CartesianSeries{TModel, TVisual, TLabel}" />
/// <seealso cref="ITimetableSeries" />
public abstract class CoreTimetableSeries<TModel, TVisual, TLabel>
    : StrokeAndFillCartesianSeries<TModel, TVisual, TLabel>, ITimetableSeries
        where TVisual : BoundedDrawnGeometry, new()
        where TLabel : BaseLabelGeometry, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoreScatterSeries{TModel, TVisual, TLabel, TErrorGeometry}"/> class.
    /// </summary>
    /// <param name="values">The values.</param>
    protected CoreTimetableSeries(IReadOnlyCollection<TModel>? values)
        : base(GetProperties(), values)
    {
        DataPadding = new LvcPoint(1, 1);

        DataLabelsFormatter = point => $"{point.Coordinate.SecondaryValue}";
        YToolTipLabelFormatter = point =>
        {
            var c = point.Coordinate;
            return $"X = {c.SecondaryValue}{Environment.NewLine}" +
                   $"Length = {c.TertiaryValue}";
        };
    }

    /// <summary>
    /// Gets or sets the minimum size of the geometry.
    /// </summary>
    /// <value>
    /// The minimum size of the geometry.
    /// </value>
    public double MinGeometrySize
    {
        get;
        set => SetProperty(ref field, value);
    } = 6d;
    /// <summary>
    /// Gets or sets the size of the geometry.
    /// </summary>
    /// <value>
    /// The size of the geometry.
    /// </value>
    public double GeometrySize
    {
        get;
        set => SetProperty(ref field, value);
    } = 24d;

    /// <inheritdoc cref="ITimetableSeries.Rx"/>
    public double Rx
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <inheritdoc cref="ITimetableSeries.Ry"/>
    public double Ry
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <inheritdoc />
    public double BarOffset
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <inheritdoc cref="ChartElement.Invalidate(Chart)"/>
    public override void Invalidate(Chart chart)
    {
        var cartesianChart = (CartesianChartEngine)chart;
        var primaryAxis = cartesianChart.GetYAxis(this);
        var secondaryAxis = cartesianChart.GetXAxis(this);

        var drawLocation = cartesianChart.DrawMarginLocation;
        var drawMarginSize = cartesianChart.DrawMarginSize;
        var xScale = secondaryAxis.GetNextScaler(cartesianChart);
        var yScale = primaryAxis.GetNextScaler(cartesianChart);

        var actualZIndex = ZIndex == 0 ? ((ISeries)this).SeriesId : ZIndex;

        if (Fill is not null && Fill != Paint.Default)
        {
            Fill.ZIndex = actualZIndex + PaintConstants.SeriesFillZIndexOffset;
            cartesianChart.Canvas.AddDrawableTask(Fill, zone: CanvasZone.DrawMargin);
        }
        if (Stroke is not null && Stroke != Paint.Default)
        {
            Stroke.ZIndex = actualZIndex + PaintConstants.SeriesStrokeZIndexOffset;
            cartesianChart.Canvas.AddDrawableTask(Stroke);
        }
        if (ShowDataLabels && DataLabelsPaint is not null && DataLabelsPaint != Paint.Default)
        {
            DataLabelsPaint.ZIndex = actualZIndex + PaintConstants.SeriesGeometryStrokeZIndexOffset;
            cartesianChart.Canvas.AddDrawableTask(DataLabelsPaint, zone: CanvasZone.DrawMargin);
        }

        var rx = (float)Rx;
        var ry = (float)Ry;

        var dls = (float)DataLabelsSize;
        var pointsCleanup = ChartPointCleanupContext.For(everFetched);

        var geometryHeight = (float)GeometrySize;
        var halfGeometryHeight = geometryHeight / 2f;

        var hasSvg = this.HasVariableSvgGeometry();

        var isFirstDraw = !chart.IsDrawn(((ISeries)this).SeriesId);
        var barOffset = (float)BarOffset;

        foreach (var point in Fetch(cartesianChart))
        {
            var coordinate = point.Coordinate;
            var visual = point.Context.Visual as TVisual;

            var x = xScale.ToPixels(coordinate.SecondaryValue);
            var y = yScale.ToPixels(coordinate.PrimaryValue);

            float geometryWidth;
            switch (coordinate.TertiaryValue)
            {
                case 0:
                    geometryWidth = geometryHeight;
                    x -= halfGeometryHeight;
                    break;
                case double.PositiveInfinity:
                    geometryWidth = chart.ControlSize.Width - x;
                    x -= barOffset;
                    break;
                case double.NegativeInfinity:
                    geometryWidth = x;
                    x -= geometryWidth - barOffset;
                    break;
                case < 0:
                    geometryWidth = x - xScale.ToPixels(coordinate.SecondaryValue + coordinate.TertiaryValue) + barOffset * 2;
                    x -= geometryWidth - barOffset;
                    break;
                default:
                    geometryWidth = xScale.ToPixels(coordinate.SecondaryValue + coordinate.TertiaryValue) - x + barOffset * 2;
                    x -= barOffset;
                    break;
            }

            if (point.IsEmpty || !IsVisible)
            {
                if (visual is not null)
                {
                    visual.X = x;
                    visual.Y = y - halfGeometryHeight;
                    visual.Width = 0;
                    visual.Height = 0;
                    visual.RemoveOnCompleted = true;
                    point.Context.Visual = null;
                }

                if (point.Context.Label is not null)
                {
                    var label = (TLabel)point.Context.Label;

                    label.X = x;
                    label.Y = y - halfGeometryHeight;
                    label.Opacity = 0;
                    label.RemoveOnCompleted = true;

                    point.Context.Label = null;
                }

                pointsCleanup.Clean(point);

                continue;
            }


            if (visual is null)
            {
                var r = new TVisual
                {
                    X = x,
                    Y = y,
                    Width = 0,
                    Height = 0
                };

                visual = r;
                point.Context.Visual = visual;
                OnPointCreated(point);

                _ = everFetched.Add(point);

                if (r is BaseRoundedRectangleGeometry rg)
                    rg.BorderRadius = new LvcPoint(rx, ry);
            }

            if (hasSvg)
            {
                var svgVisual = (IVariableSvgPath)visual;
                if (_geometrySvgChanged || svgVisual.SVGPath is null)
                    svgVisual.SVGPath = GeometrySvg ?? throw new Exception("svg path is not defined");
            }

            if (Fill is not null && Fill != Paint.Default)
                Fill.AddGeometryToPaintTask(cartesianChart.Canvas, visual);
            if (Stroke is not null && Stroke != Paint.Default)
                Stroke.AddGeometryToPaintTask(cartesianChart.Canvas, visual);

            var sizedGeometry = visual;

            sizedGeometry.X = x;
            sizedGeometry.Y = y - halfGeometryHeight;
            sizedGeometry.Width = geometryWidth;
            sizedGeometry.Height = geometryHeight;

            if (sizedGeometry is BaseRoundedRectangleGeometry rrg)
                rrg.BorderRadius = new LvcPoint(rx, ry);

            sizedGeometry.RemoveOnCompleted = false;

            if (point.Context.HoverArea is not RectangleHoverArea ha)
                point.Context.HoverArea = ha = new RectangleHoverArea();
            _ = ha.SetDimensions(x, y - halfGeometryHeight, geometryWidth, geometryHeight).CenterXToolTip().CenterYToolTip();

            pointsCleanup.Clean(point);

            if (ShowDataLabels && DataLabelsPaint is not null && DataLabelsPaint != Paint.Default)
            {
                if (point.Context.Label is not TLabel label)
                {
                    var l = new TLabel
                    {
                        X = x,
                        Y = y - halfGeometryHeight,
                        RotateTransform = (float)DataLabelsRotation,
                        MaxWidth = (float)DataLabelsMaxWidth
                    };
                    l.Animate(GetAnimation(cartesianChart),
                        BaseLabelGeometry.XProperty,
                        BaseLabelGeometry.YProperty);
                    label = l;
                    point.Context.Label = l;
                }

                DataLabelsPaint.AddGeometryToPaintTask(cartesianChart.Canvas, label);
                label.Text = DataLabelsFormatter(new ChartPoint<TModel, TVisual, TLabel>(point));
                label.TextSize = dls;
                label.Padding = DataLabelsPadding;
                label.Paint = DataLabelsPaint;

                if (isFirstDraw)
                    label.CompleteTransition(
                        BaseLabelGeometry.TextSizeProperty,
                        BaseLabelGeometry.XProperty,
                        BaseLabelGeometry.YProperty,
                        BaseLabelGeometry.RotateTransformProperty);

                var m = label.Measure();
                var labelPosition = GetLabelPosition(
                    x, y - halfGeometryHeight, geometryWidth, geometryHeight, m, DataLabelsPosition,
                    SeriesProperties, coordinate.PrimaryValue > 0, drawLocation, drawMarginSize);
                if (DataLabelsTranslate is not null) label.TranslateTransform =
                        new LvcPoint(m.Width * DataLabelsTranslate.Value.X, m.Height * DataLabelsTranslate.Value.Y);
                label.X = labelPosition.X;
                label.Y = labelPosition.Y;
            }

            OnPointMeasured(point);
        }

        pointsCleanup.CollectPoints(everFetched, cartesianChart.View, yScale, xScale, SoftDeleteOrDisposePoint);
        _geometrySvgChanged = false;
    }

    /// <inheritdoc cref="CartesianSeries{TModel, TVisual, TLabel}.GetBounds(Chart, ICartesianAxis, ICartesianAxis)"/>
    public override SeriesBounds GetBounds(
        Chart chart, ICartesianAxis secondaryAxis, ICartesianAxis primaryAxis)
    {
        var rawBounds = DataFactory.GetTimetableBounds(chart, this, secondaryAxis, primaryAxis);
        if (rawBounds.HasData) return rawBounds;

        var rawBaseBounds = rawBounds.Bounds;

        var tickPrimary = primaryAxis.GetTick(chart.ControlSize, rawBaseBounds.VisiblePrimaryBounds);
        var tickSecondary = secondaryAxis.GetTick(chart.ControlSize, rawBaseBounds.VisibleSecondaryBounds);

        var ts = tickSecondary.Value * DataPadding.X;
        var tp = tickPrimary.Value * DataPadding.Y;

        var rgs = GetRequestedGeometrySize();
        var rso = GetRequestedSecondaryOffset();
        var rpo = GetRequestedPrimaryOffset();

        var dimensionalBounds = new DimensionalBounds
        {
            SecondaryBounds = new Bounds
            {
                Max = rawBaseBounds.SecondaryBounds.Max + rso * secondaryAxis.UnitWidth,
                Min = rawBaseBounds.SecondaryBounds.Min - rso * secondaryAxis.UnitWidth,
                MinDelta = rawBaseBounds.SecondaryBounds.MinDelta,
                PaddingMax = ts,
                PaddingMin = ts,
                RequestedGeometrySize = rgs
            },
            PrimaryBounds = new Bounds
            {
                Max = rawBaseBounds.PrimaryBounds.Max + rpo * secondaryAxis.UnitWidth,
                Min = rawBaseBounds.PrimaryBounds.Min - rpo * secondaryAxis.UnitWidth,
                MinDelta = rawBaseBounds.PrimaryBounds.MinDelta,
                PaddingMax = tp,
                PaddingMin = tp,
                RequestedGeometrySize = rgs
            },
            VisibleSecondaryBounds = new Bounds
            {
                Max = rawBaseBounds.VisibleSecondaryBounds.Max + rso * secondaryAxis.UnitWidth,
                Min = rawBaseBounds.VisibleSecondaryBounds.Min - rso * secondaryAxis.UnitWidth,
            },
            VisiblePrimaryBounds = new Bounds
            {
                Max = rawBaseBounds.VisiblePrimaryBounds.Max + rpo * secondaryAxis.UnitWidth,
                Min = rawBaseBounds.VisiblePrimaryBounds.Min - rpo * secondaryAxis.UnitWidth
            },
            TertiaryBounds = rawBaseBounds.TertiaryBounds,
            VisibleTertiaryBounds = rawBaseBounds.VisibleTertiaryBounds
        };

        return new SeriesBounds(dimensionalBounds, false);
    }

    /// <inheritdoc />
    protected override double GetRequestedPrimaryOffset() => 0.5;

    /// <inheritdoc cref="Series{TModel, TVisual, TLabel}.GetMiniatureGeometry(ChartPoint)"/>
    public override IDrawnElement GetMiniatureGeometry(ChartPoint? point)
    {
        var v = point?.Context.Visual;

        var m = new TVisual
        {
            Fill = v?.Fill ?? Fill,
            Stroke = v?.Stroke ?? Stroke,
            Width = (float)MiniatureShapeSize,
            Height = (float)MiniatureShapeSize,
            ClippingBounds = LvcRectangle.Empty,
            RotateTransform = v?.RotateTransform ?? 0
        };

        if (m is IVariableSvgPath svg) svg.SVGPath = GeometrySvg;

        return m;
    }

    /// <inheritdoc cref="Series{TModel, TVisual, TLabel}.OnPointerEnter(ChartPoint)"/>
    protected override void OnPointerEnter(ChartPoint point)
    {
        var visual = (TVisual?)point.Context.Visual;
        if (visual is null) return;
        visual.Opacity = 0.8f;

        base.OnPointerEnter(point);
    }

    /// <inheritdoc cref="Series{TModel, TVisual, TLabel}.OnPointerLeft(ChartPoint)"/>
    protected override void OnPointerLeft(ChartPoint point)
    {
        var visual = (TVisual?)point.Context.Visual;
        if (visual is null) return;
        visual.Opacity = 1;

        base.OnPointerLeft(point);
    }

    /// <inheritdoc cref="SetDefaultPointTransitions(ChartPoint)"/>
    protected override void SetDefaultPointTransitions(ChartPoint chartPoint)
    {
        var chart = chartPoint.Context.Chart;
        if (chartPoint.Context.Visual is not TVisual visual) throw new Exception("Unable to initialize the point instance.");

        var animation = GetAnimation(chart.CoreChart);

        visual.Animate(animation);
    }

    /// <inheritdoc cref="SoftDeleteOrDisposePoint(ChartPoint, Scaler, Scaler)"/>
    protected internal override void SoftDeleteOrDisposePoint(ChartPoint point, Scaler primaryScale, Scaler secondaryScale)
    {
        var visual = (TVisual?)point.Context.Visual;
        if (visual is null) return;
        if (DataFactory is null) throw new Exception("Data provider not found");

        var c = point.Coordinate;

        var x = secondaryScale.ToPixels(c.SecondaryValue);
        var y = primaryScale.ToPixels(c.PrimaryValue);

        visual.X = x;
        visual.Y = y;
        visual.Height = 0;
        visual.Width = 0;
        visual.RemoveOnCompleted = true;

        DataFactory.DisposePoint(point);

        var label = (TLabel?)point.Context.Label;
        if (label is null) return;

        label.TextSize = 1;
        label.RemoveOnCompleted = true;
    }

    private static SeriesProperties GetProperties() =>
        SeriesProperties.Timetable | SeriesProperties.Solid | SeriesProperties.PrefersXYStrategyTooltips |
        SeriesProperties.PrimaryAxisHorizontalOrientation;
}
