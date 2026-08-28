using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeliveryBot.World
{
    /// <summary>
    /// Pure-C# description of the city grid: intersection nodes on a regular lattice,
    /// road edges between neighbours, right-hand lanes, and block sidewalk loops.
    /// Shared by traffic, pedestrians, delivery spawning and the scene builder.
    /// </summary>
    public sealed class RoadGraph
    {
        public readonly int Blocks;
        public readonly float BlockSize;
        public readonly float RoadWidth;
        public readonly float LaneOffset;
        public readonly float SidewalkWidth;

        public float Pitch => BlockSize + RoadWidth;
        public float Half => Blocks * Pitch * 0.5f;
        public int NodesPerAxis => Blocks + 1;

        public RoadGraph(int blocks, float blockSize, float roadWidth, float laneOffset, float sidewalkWidth)
        {
            Blocks = blocks;
            BlockSize = blockSize;
            RoadWidth = roadWidth;
            LaneOffset = laneOffset;
            SidewalkWidth = sidewalkWidth;
        }

        public readonly struct Node : IEquatable<Node>
        {
            public readonly int I, J;
            public Node(int i, int j) { I = i; J = j; }
            public bool Equals(Node o) => I == o.I && J == o.J;
            public override bool Equals(object obj) => obj is Node n && Equals(n);
            public override int GetHashCode() => I * 397 ^ J;
            public override string ToString() => $"({I},{J})";
        }

        public Vector3 NodePosition(Node n) => new Vector3(-Half + n.I * Pitch, 0f, -Half + n.J * Pitch);

        public bool IsValid(Node n) => n.I >= 0 && n.J >= 0 && n.I < NodesPerAxis && n.J < NodesPerAxis;

        public IEnumerable<Node> AllNodes()
        {
            for (var i = 0; i < NodesPerAxis; i++)
            for (var j = 0; j < NodesPerAxis; j++)
                yield return new Node(i, j);
        }

        public List<Node> Neighbors(Node n)
        {
            var list = new List<Node>(4);
            foreach (var c in new[] { new Node(n.I + 1, n.J), new Node(n.I - 1, n.J), new Node(n.I, n.J + 1), new Node(n.I, n.J - 1) })
                if (IsValid(c)) list.Add(c);
            return list;
        }

        /// <summary>Random next node that is not a U-turn (unless the node is a dead end).</summary>
        public Node NextNode(Node previous, Node current, System.Random rng)
        {
            var options = Neighbors(current);
            if (options.Count > 1) options.Remove(previous);
            return options[rng.Next(options.Count)];
        }

        /// <summary>Point along the right-hand lane of the edge from → to, t in [0,1].</summary>
        public Vector3 LanePoint(Node from, Node to, float t)
        {
            var a = NodePosition(from);
            var b = NodePosition(to);
            var dir = (b - a).normalized;
            var right = Vector3.Cross(Vector3.up, dir);
            return Vector3.Lerp(a, b, t) + right * LaneOffset;
        }

        public Vector3 EdgeDirection(Node from, Node to) => (NodePosition(to) - NodePosition(from)).normalized;
        public float EdgeLength => Pitch;

        public Vector3 BlockCenter(int bx, int bz) => new Vector3(-Half + Pitch * 0.5f + bx * Pitch, 0f, -Half + Pitch * 0.5f + bz * Pitch);

        /// <summary>Four corners of the sidewalk centre-line loop around a block (clockwise seen from above).</summary>
        public Vector3[] SidewalkLoop(int bx, int bz)
        {
            var c = BlockCenter(bx, bz);
            var e = BlockSize * 0.5f - SidewalkWidth * 0.5f;
            return new[]
            {
                c + new Vector3(-e, 0f, -e), c + new Vector3(-e, 0f, e),
                c + new Vector3(e, 0f, e), c + new Vector3(e, 0f, -e)
            };
        }

        /// <summary>Position on the sidewalk in front of a storefront: middle of a block side, just inside the curb.</summary>
        public Vector3 StorefrontPoint(int bx, int bz, int side)
        {
            var c = BlockCenter(bx, bz);
            var edge = BlockSize * 0.5f - SidewalkWidth * 0.35f;
            return side switch
            {
                0 => c + new Vector3(0f, 0f, edge),
                1 => c + new Vector3(edge, 0f, 0f),
                2 => c + new Vector3(0f, 0f, -edge),
                _ => c + new Vector3(-edge, 0f, 0f)
            };
        }

        /// <summary>Outward normal of a block side (0=N,1=E,2=S,3=W).</summary>
        public static Vector3 SideNormal(int side) => side switch
        {
            0 => Vector3.forward,
            1 => Vector3.right,
            2 => Vector3.back,
            _ => Vector3.left
        };
    }
}
