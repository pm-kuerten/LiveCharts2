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

using LiveChartsCore.Defaults;

namespace ViewModelsSamples.Timetable.Basic;

public class ViewModel
{
    public WeightedPoint[] Values1 { get; }
    public WeightedPoint[] Values2 { get; }
    public WeightedPoint[] Values3 { get; }

    public ViewModel()
    {
        var r = new Random();
        Values1 = [.. Enumerable.Range(0, 10).Select(_ => new WeightedPoint(r.Next(0, 20), r.Next(0, 20), r.Next(0, 5)))];
        Values2 = [.. Enumerable.Range(0, 10).Select(_ => new WeightedPoint(r.Next(0, 20), r.Next(0, 20), r.Next(0, 5)))];
        Values3 = [.. Enumerable.Range(0, 10).Select(_ => new WeightedPoint(r.Next(0, 20), r.Next(0, 20), r.Next(0, 5)))];
    }
}
