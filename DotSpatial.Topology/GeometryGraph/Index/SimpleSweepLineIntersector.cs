// ********************************************************************************************************
// Product Name: DotSpatial.Topology.dll
// Description:  The basic topology module for the new dotSpatial libraries
// ********************************************************************************************************
// The contents of this file are subject to the Lesser GNU Public License (LGPL)
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://dotspatial.codeplex.com/license  Alternately, you can access an earlier version of this content from
// the Net Topology Suite, which is also protected by the GNU Lesser Public License and the sourcecode
// for the Net Topology Suite can be obtained here: http://sourceforge.net/projects/nts.
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF
// ANY KIND, either expressed or implied. See the License for the specific language governing rights and
// limitations under the License.
//
// The Original Code is from the Net Topology Suite, which is a C# port of the Java Topology Suite.
//
// The Initial Developer to integrate this code into MapWindow 6.0 is Ted Dunsford.
//
// Contributor(s): (Open source contributors should list themselves and their modifications here).
// |         Name         |    Date    |                              Comment
// |----------------------|------------|------------------------------------------------------------
// |                      |            |
// ********************************************************************************************************

using System.Collections;
using System.Collections.Generic;

namespace DotSpatial.Topology.GeometriesGraph.Index
{
    /// <summary>
    /// Finds all intersections in one or two sets of edges,
    /// using a simple x-axis sweepline algorithm.
    /// While still O(n^2) in the worst case, this algorithm
    /// drastically improves the average-case time.
    /// </summary>
    public class SimpleSweepLineIntersector : EdgeSetIntersector
    {
        private readonly ArrayList _events = new ArrayList();

        // statistics information
        int _nOverlaps;

        /// <summary>
        ///
        /// </summary>
        /// <param name="edges"></param>
        /// <param name="si"></param>
        /// <param name="testAllSegments"></param>
        public override void ComputeIntersections(IList edges, SegmentIntersector si, bool testAllSegments)
        {
            if (testAllSegments)
                Add(edges, null);
            else Add(edges);
            ComputeIntersections(si);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="edges0"></param>
        /// <param name="edges1"></param>
        /// <param name="si"></param>
        public override void ComputeIntersections(IList edges0, IList edges1, SegmentIntersector si)
        {
            Add(edges0, edges0);
            Add(edges1, edges1);
            ComputeIntersections(si);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="edges"></param>
        private void Add(IEnumerable edges)
        {
            for (IEnumerator i = edges.GetEnumerator(); i.MoveNext(); )
            {
                Edge edge = (Edge)i.Current;
                // edge is its own group
                Add(edge, edge);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="edges"></param>
        /// <param name="edgeSet"></param>
        private void Add(IEnumerable edges, object edgeSet)
        {
            for (IEnumerator i = edges.GetEnumerator(); i.MoveNext(); )
            {
                Edge edge = (Edge)i.Current;
                Add(edge, edgeSet);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="edgeSet"></param>
        private void Add(Edge edge, object edgeSet)
        {
            IList<Coordinate> pts = edge.Coordinates;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                SweepLineSegment ss = new SweepLineSegment(edge, i);
                SweepLineEvent insertEvent = new SweepLineEvent(edgeSet, ss.MinX, null, ss);
                _events.Add(insertEvent);
                _events.Add(new SweepLineEvent(edgeSet, ss.MaxX, insertEvent, ss));
            }
        }

        /// <summary>
        /// Because Delete Events have a link to their corresponding Insert event,
        /// it is possible to compute exactly the range of events which must be
        /// compared to a given Insert event object.
        /// </summary>
        private void PrepareEvents()
        {
            _events.Sort();
            for (int i = 0; i < _events.Count; i++)
            {
                SweepLineEvent ev = (SweepLineEvent)_events[i];
                if (ev.IsDelete)
                    ev.InsertEvent.DeleteEventIndex = i;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="si"></param>
        private void ComputeIntersections(SegmentIntersector si)
        {
            _nOverlaps = 0;
            PrepareEvents();

            for (int i = 0; i < _events.Count; i++)
            {
                SweepLineEvent ev = (SweepLineEvent)_events[i];
                if (ev.IsInsert)
                    ProcessOverlaps(i, ev.DeleteEventIndex, ev, si);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="ev0"></param>
        /// <param name="si"></param>
        private void ProcessOverlaps(int start, int end, SweepLineEvent ev0, SegmentIntersector si)
        {
            SweepLineSegment ss0 = (SweepLineSegment)ev0.Object;
            /*
            * Since we might need to test for self-intersections,
            * include current insert event object in list of event objects to test.
            * Last index can be skipped, because it must be a Delete event.
            */
            for (int i = start; i < end; i++)
            {
                SweepLineEvent ev1 = (SweepLineEvent)_events[i];
                if (ev1.IsInsert)
                {
                    SweepLineSegment ss1 = (SweepLineSegment)ev1.Object;
                    if (ev0.EdgeSet == null || (ev0.EdgeSet != ev1.EdgeSet))
                        ss0.ComputeIntersections(ss1, si);
                    _nOverlaps++;
                }
            }
        }
    }
}