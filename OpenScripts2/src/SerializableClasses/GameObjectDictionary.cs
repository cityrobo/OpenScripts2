using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class GameObjectDictionary : OpenScripts2_BasePlugin
    {
        [Serializable]
        public class GameObjectDictionaryEntry
        {
            public string ID;
            public GameObject GameObject;
            public GameObjectDictionaryEntry(string key, GameObject value)
            {
                ID = key;
                GameObject = value;
            }
        }

        public List<GameObjectDictionaryEntry> GameObjectDictionaryEntries;

        private Dictionary<string, GameObject> _gameObjectDict = new();

        private bool _isInitialized = false;

        public void Awake()
        {
            if (!_isInitialized) UpdateDictionary();
        }

        public GameObject GetGameObject(string id)
        {
            if (!_isInitialized) UpdateDictionary();
            if (_gameObjectDict.TryGetValue(id, out var gameObject))
            {
                return gameObject;
            }
            return null;
        }

        public bool TryGetGameObject(string id, out GameObject gameObject)
        {
            if (!_isInitialized) UpdateDictionary();
            return _gameObjectDict.TryGetValue(id, out gameObject);
        }

        public void UpdateDictionary()
        {
            _gameObjectDict.Clear();
            foreach (var entry in GameObjectDictionaryEntries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.ID) && entry.GameObject != null && !_gameObjectDict.ContainsKey(entry.ID))
                {
                    _gameObjectDict.Add(entry.ID, entry.GameObject);
                }
                else if (entry == null)
                {
                    LogWarning("Null entry found in GameObjectDistionaryEntries!");
                }
                else if (string.IsNullOrEmpty(entry.ID))
                {
                    LogWarning("Entry with empty ID found in GameObjectDictionaryEntries!");
                }
                else if (entry.GameObject == null)
                {
                    LogWarning($"Entry with ID '{entry.ID}' has an empty Gameobject field!");
                }
                else if (_gameObjectDict.ContainsKey(entry.ID))
                {
                    LogWarning($"Duplicate ID '{entry.ID}' found in GameObjectDictionaryEntries!");
                }
            }
            _isInitialized = true;
        }
    }
}
