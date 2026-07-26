using UnityEngine;

[CreateAssetMenu(fileName = "FileSpawnTable", menuName = "Game/File Spawn Table")]
public class FileSpawnTable : ScriptableObject
{
    [System.Serializable]
    public struct SpawnEntry
    {
        public GameObject prefab;

        [Min(0f)]
        public float weight;
    }

    [SerializeField] private SpawnEntry[] _entries;

    // Cache
    private float[] _cumulativeWeights;
    private float _totalWeight;
    private bool _isBuilt;

    private void BuildIfNeeded()
    {
        if (_isBuilt) return;

        _cumulativeWeights = new float[_entries.Length];
        float cumulative = 0f;

        for (int i = 0; i < _entries.Length; i++)
        {
            cumulative += _entries[i].weight;
            _cumulativeWeights[i] = cumulative;
        }

        _totalWeight = cumulative;
        _isBuilt = true;
    }

    public GameObject Pick()
    {
        BuildIfNeeded();

        if (_totalWeight <= 0f || _entries.Length == 0)
        {
            Debug.LogWarning($"[{name}] Spawn table ว่างหรือ weight เป็น 0");
            return null;
        }

        float randomPoint = Random.Range(0f, _totalWeight);
        int low = 0, high = _cumulativeWeights.Length - 1;

        while (low < high)
        {
            int mid = (low + high) / 2;
            if (_cumulativeWeights[mid] < randomPoint)
                low = mid + 1;
            else
                high = mid;
        }

        return _entries[low].prefab;
    }

    private void OnValidate() => _isBuilt = false;
}