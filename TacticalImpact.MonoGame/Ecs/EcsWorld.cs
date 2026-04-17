using System.Collections;

namespace TacticalImpact.MonoGame.Ecs;

public sealed class EcsWorld
{
    private readonly HashSet<int> _entities = [];
    private readonly Dictionary<Type, IComponentPool> _pools = [];
    private int _nextEntityId = 1;

    public int CreateEntity()
    {
        var entityId = _nextEntityId++;
        _entities.Add(entityId);
        return entityId;
    }

    public void DestroyEntity(int entity)
    {
        if (!_entities.Remove(entity))
        {
            return;
        }

        foreach (var pool in _pools.Values)
        {
            pool.Remove(entity);
        }
    }

    public void AddComponent<T>(int entity, T component) where T : class
    {
        ValidateEntity(entity);
        var pool = GetOrCreatePool<T>();
        pool.Set(entity, component);
    }

    public bool HasComponent<T>(int entity) where T : class
    {
        if (!_entities.Contains(entity))
        {
            return false;
        }

        return TryGetPool<T>(out var pool) && pool.Has(entity);
    }

    public T GetComponent<T>(int entity) where T : class
    {
        ValidateEntity(entity);
        if (!TryGetPool<T>(out var pool) || !pool.TryGet(entity, out var component) || component is null)
        {
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T).Name}.");
        }

        return component;
    }

    public IEnumerable<int> Query<T>() where T : class
    {
        if (!TryGetPool<T>(out var pool))
        {
            yield break;
        }

        foreach (var entity in _entities)
        {
            if (pool.Has(entity))
            {
                yield return entity;
            }
        }
    }

    public IEnumerable<int> Query<T1, T2>()
        where T1 : class
        where T2 : class
    {
        if (!TryGetPool<T1>(out var pool1) || !TryGetPool<T2>(out var pool2))
        {
            yield break;
        }

        foreach (var entity in _entities)
        {
            if (pool1.Has(entity) && pool2.Has(entity))
            {
                yield return entity;
            }
        }
    }

    public IEnumerable<int> Query<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class
    {
        if (!TryGetPool<T1>(out var pool1) || !TryGetPool<T2>(out var pool2) || !TryGetPool<T3>(out var pool3))
        {
            yield break;
        }

        foreach (var entity in _entities)
        {
            if (pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity))
            {
                yield return entity;
            }
        }
    }

    private void ValidateEntity(int entity)
    {
        if (!_entities.Contains(entity))
        {
            throw new InvalidOperationException($"Entity {entity} does not exist.");
        }
    }

    private ComponentPool<T> GetOrCreatePool<T>() where T : class
    {
        if (TryGetPool<T>(out var pool))
        {
            return pool;
        }

        var newPool = new ComponentPool<T>();
        _pools[typeof(T)] = newPool;
        return newPool;
    }

    private bool TryGetPool<T>(out ComponentPool<T> pool) where T : class
    {
        if (_pools.TryGetValue(typeof(T), out var existingPool) && existingPool is ComponentPool<T> typedPool)
        {
            pool = typedPool;
            return true;
        }

        pool = null!;
        return false;
    }

    private interface IComponentPool
    {
        bool Has(int entity);
        void Remove(int entity);
    }

    private sealed class ComponentPool<T> : IComponentPool, IEnumerable<KeyValuePair<int, T>> where T : class
    {
        private readonly Dictionary<int, T> _components = [];

        public bool Has(int entity) => _components.ContainsKey(entity);

        public bool TryGet(int entity, out T? component)
        {
            return _components.TryGetValue(entity, out component);
        }

        public void Set(int entity, T component)
        {
            _components[entity] = component;
        }

        public void Remove(int entity)
        {
            _components.Remove(entity);
        }

        public IEnumerator<KeyValuePair<int, T>> GetEnumerator() => _components.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}