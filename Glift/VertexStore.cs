using System;
using System.Linq;
using System.Collections.Generic;

using Point3 = System.Numerics.Vector3;

namespace Glift {
    using VCacheList = List<(Point3 Point, int Index)>;
    using VCacheDict = Dictionary<Point3, int>;

    public class VertexStore {
        private VCacheDict _vert_d;
        private VCacheList _vert_l;
        private Type _containType;
        private int _idx;

        public enum Type { List, Dict }

        private void _MigrateToDict() {
            if (_containType == Type.Dict)
                return;
            foreach (var entry in _vert_l)
                _vert_d[entry.Point] = entry.Index;
            _vert_l.Clear();
            _containType = Type.Dict;
        }

        private void _MigrateToList() {
            if (_containType == Type.List)
                return;
            foreach (var entry in _vert_d.OrderBy(e => e.Value))
                _vert_l.Add((entry.Key, entry.Value));
            _vert_d.Clear();
            _containType = Type.List;
        }

        public VertexStore(Type containType = Type.List) {
            _containType = containType;
            _idx = 0;
            _vert_d = new VCacheDict();
            _vert_l = new VCacheList();
        }

        public VertexStore.Type ContainerType {
            get => _containType;
            set {
                if (value == Type.List)
                    _MigrateToList();
                else
                    _MigrateToDict();
            }
        }

        public void Add(Point3 vertex) {
            if (_containType == Type.Dict)
                _vert_d[vertex] = ++_idx;
            else
                _vert_l.Add((vertex, ++_idx));
        }

        public bool Contains(Point3 vertex) {
            if (_containType == Type.Dict)
                return _vert_d.ContainsKey(vertex);
            try { return IndexOf(vertex) >= 0; }
            catch (VertexNotFoundException) { return false; }
        }

        public int IndexOf(Point3 vertex) {
            if (_containType == Type.Dict) {
                try { return _vert_d[vertex]; }
                catch (KeyNotFoundException) {
                    throw new VertexNotFoundException(vertex);
                }
            }

            foreach (var e in _vert_l)
                if (vertex.EqualsEps(e.Point))
                    return e.Index;
            throw new VertexNotFoundException(vertex);
        }

        public IEnumerable<Point3> Vertices {
            get {
                if (_containType == Type.Dict)
                    foreach (var entry in _vert_d.OrderBy(e => e.Value))
                        yield return entry.Key;
                else
                    foreach (var entry in _vert_l)
                        yield return entry.Point;
            }
        }
    }
}
