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

namespace LiveChartsCore.Kernel.Sketches;

/// <summary>
/// Defines a timetable series.
/// </summary>
public interface ITimetableSeries : ICartesianSeries, IStrokedAndFilled
{
    /// <summary>
    /// Gets or sets the rx, the radius used in the x-axis to round the corners of each bar in pixels.
    /// </summary>
    double Rx { get; set; }

    /// <summary>
    /// Gets or sets the ry, the radius used in the y-axis to round the corners of each bar in pixels.
    /// </summary>
    double Ry { get; set; }

    /// <summary>
    /// Offset to extend or shrink each bar at their ends in pixels.
    /// </summary>
    double BarOffset { get; set; }
}
