﻿namespace Apex.AI.Teaching
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class NestStructure : MonoBehaviour, ICanDie
    {
        [SerializeField]
        private float _maxHealth = 1000f;

        [SerializeField]
        private float _spawnDistance = 10f;

        [SerializeField]
        private float _buildCooldown = 0.5f;

        [SerializeField]
        private int _startHarvesters = 3;

        [SerializeField]
        private int _startWarriors = 2;

        [SerializeField]
        private int _startBlasters = 1;

        [SerializeField]
        private int _initialInstanceCount = 30;

        [SerializeField]
        private GameObject _harvesterPrefab;

        [SerializeField]
        private GameObject _blasterPrefab;

        [SerializeField]
        private GameObject _warriorPrefab;

        private Dictionary<UnitType, UnitPool> _entityPools;
        private float _lastBuild;

        /// <summary>
        /// Gets a reference to the AI controller - DO NOT MODIFY.
        /// </summary>
        /// <value>
        /// The controller.
        /// </value>
        public AIController controller
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the maximum health.
        /// </summary>
        /// <value>
        /// The maximum health.
        /// </value>
        public float maxHealth
        {
            get { return _maxHealth; }
        }

        /// <summary>
        /// Gets the current health.
        /// </summary>
        /// <value>
        /// The current health.
        /// </value>
        public float currentHealth
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether this nest is dead.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this nest is dead; otherwise, <c>false</c>.
        /// </value>
        public bool isDead
        {
            get { return this.currentHealth <= 0f; }
        }

        /// <summary>
        /// Gets the harvester count - the currently active amount of harvester units.
        /// </summary>
        /// <value>
        /// The harvester count.
        /// </value>
        public int harvesterCount
        {
            get { return _entityPools[UnitType.Harvester].count; }
        }

        /// <summary>
        /// Gets the warrior count - the currently active amount of warrior units.
        /// </summary>
        /// <value>
        /// The warrior count.
        /// </value>
        public int warriorCount
        {
            get { return _entityPools[UnitType.Warrior].count; }
        }

        /// <summary>
        /// Gets the blaster count - the currently active amount of blaster units.
        /// </summary>
        /// <value>
        /// The blaster count.
        /// </value>
        public int blasterCount
        {
            get { return _entityPools[UnitType.Blaster].count; }
        }

        private void Awake()
        {
            _entityPools = new Dictionary<UnitType, UnitPool>(new UnitTypeComparer())
            {
                { UnitType.Harvester, new UnitPool(_harvesterPrefab, this.gameObject, _initialInstanceCount) },
                { UnitType.Blaster, new UnitPool(_blasterPrefab, this.gameObject, _initialInstanceCount) },
                { UnitType.Warrior, new UnitPool(_warriorPrefab, this.gameObject, _initialInstanceCount) }
            };
        }

        private void OnEnable()
        {
            this.currentHealth = _maxHealth;
            StartCoroutine(BuildInitialUnits());
        }

        private void OnDisable()
        {
            var units = this.controller.units;
            var count = units.Count;
            for (int i = 0; i < count; i++)
            {
                // all the nest's units die when the nest dies
                units[i].ReceiveDamage(units[i].maxHealth + 1f);
                ReturnUnit(units[i]);
            }
        }

        private IEnumerator BuildInitialUnits()
        {
            if (_startHarvesters > 0)
            {
                yield return new WaitForSeconds(1f);
                StartCoroutine(BuildUnitsGradually(_startHarvesters, UnitType.Harvester));
            }

            if (_startWarriors > 0)
            {
                yield return new WaitForSeconds(1f);
                StartCoroutine(BuildUnitsGradually(_startWarriors, UnitType.Warrior));
            }

            if (_startBlasters > 0)
            {
                yield return new WaitForSeconds(1f);
                StartCoroutine(BuildUnitsGradually(_startBlasters, UnitType.Blaster));
            }
        }

        private IEnumerator BuildUnitsGradually(int count, UnitType type)
        {
            for (int i = 0; i < count; i++)
            {
                InternalBuildUnit(type);
                yield return new WaitForSeconds(_buildCooldown);
            }
        }

        /// <summary>
        /// Builds a unit of the specified type. Automatically consumes resources, and can only execute as often as permitted by the build cooldown (0.5 seconds).
        /// </summary>
        /// <param name="type">The type of unit to spawn.</param>
        public void SpawnUnit(UnitType type)
        {
            if (type == UnitType.None)
            {
                Debug.LogError(this.ToString() + " cannot build units of type 'None'");
                return;
            }

            var cost = UnitCostManager.GetCost(type);
            if (cost > this.controller.currentResources)
            {
                // AI cannot afford the unit
                return;
            }

            var time = Time.time;
            if (time - _lastBuild < _buildCooldown)
            {
                // cooldown still in effect
                return;
            }

            _lastBuild = time;
            this.controller.currentResources -= cost;
            InternalBuildUnit(type);
        }

        private void InternalBuildUnit(UnitType type)
        {
            var pos = this.transform.position + (UnityEngine.Random.onUnitSphere.normalized * _spawnDistance);
            pos.y = this.transform.position.y;

            var unit = _entityPools[type].Get(pos, Quaternion.identity);
            unit.nest = this;

            // color unit
            var color = this.controller.color;
            var renderers = unit.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].GetComponent<ParticleSystem>() == null)
                {
                    renderers[i].material.color = color;
                }
            }

            this.controller.units.Add(unit);
        }

        /// <summary>
        /// Returns the unit back to the unit pool, effectively deactivates it and prepares it for recycling.
        /// </summary>
        /// <param name="unit">The unit.</param>
        public void ReturnUnit(UnitBase unit)
        {
            if (unit.type == UnitType.None)
            {
                Debug.LogError(this.ToString() + " cannot return units of type 'None'");
                return;
            }

            this.controller.units.Remove(unit);
            _entityPools[unit.type].Return(unit);
        }

        /// <summary>
        /// Receive the specified amount of damage.
        /// </summary>
        /// <param name="dmg">The damage.</param>
        public void ReceiveDamage(float dmg)
        {
            this.currentHealth -= dmg;
            if (this.currentHealth <= 0)
            {
                this.gameObject.SetActive(false);
            }
        }
    }
}