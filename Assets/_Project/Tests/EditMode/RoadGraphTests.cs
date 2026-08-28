using DeliveryBot.World;
using NUnit.Framework;
using UnityEngine;

namespace DeliveryBot.Tests
{
    public class RoadGraphTests
    {
        private static RoadGraph Make() => new RoadGraph(6, 24f, 10f, 2.6f, 2.5f);

        [Test]
        public void CornerHasTwoNeighbors_CentreHasFour()
        {
            var g = Make();
            Assert.AreEqual(2, g.Neighbors(new RoadGraph.Node(0, 0)).Count);
            Assert.AreEqual(4, g.Neighbors(new RoadGraph.Node(3, 3)).Count);
        }

        [Test]
        public void NextNode_NeverUTurns_WhenAlternativesExist()
        {
            var g = Make();
            var rng = new System.Random(3);
            var prev = new RoadGraph.Node(2, 3);
            var cur = new RoadGraph.Node(3, 3);
            for (var i = 0; i < 50; i++)
                Assert.AreNotEqual(prev, g.NextNode(prev, cur, rng));
        }

        [Test]
        public void NextNode_UTurnsAtDeadEnd()
        {
            var g = new RoadGraph(1, 24f, 10f, 2.6f, 2.5f); // 2x2 nodes; corner (0,0) has 2 neighbours
            var rng = new System.Random(1);
            var prev = new RoadGraph.Node(1, 0);
            var cur = new RoadGraph.Node(0, 0);
            var next = g.NextNode(prev, cur, rng);
            Assert.IsTrue(g.IsValid(next));
        }

        [Test]
        public void LanePoint_IsOffsetToTheRight()
        {
            var g = Make();
            var from = new RoadGraph.Node(0, 0);
            var to = new RoadGraph.Node(1, 0); // heading +x, right = -z
            var mid = g.LanePoint(from, to, 0.5f);
            var centre = (g.NodePosition(from) + g.NodePosition(to)) * 0.5f;
            Assert.AreEqual(centre.x, mid.x, 1e-3f);
            Assert.AreEqual(centre.z - 2.6f, mid.z, 1e-3f);
        }

        [Test]
        public void SidewalkLoop_IsInsideBlock()
        {
            var g = Make();
            var c = g.BlockCenter(2, 2);
            foreach (var p in g.SidewalkLoop(2, 2))
            {
                Assert.LessOrEqual(Mathf.Abs(p.x - c.x), g.BlockSize * 0.5f);
                Assert.LessOrEqual(Mathf.Abs(p.z - c.z), g.BlockSize * 0.5f);
            }
        }
    }
}
