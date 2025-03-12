
using Model;
using Model.Runtime.Projectiles;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utilities;

namespace UnitBrains.Player
{
    public class SecondUnitBrain : DefaultPlayerUnitBrain
    {
        public override string TargetUnitName => "Cobra Commando";
        private const float OverheatTemperature = 3f;
        private const float OverheatCooldown = 2f;
        private float _temperature = 0f;
        private float _cooldownTime = 0f;
        private bool _overheated;
        private List<Vector2Int> _outOfRangeTargets = new(); // поле, со списком недосягаемых целей
        private static int _unitCounter = 0;
        private int _unitId = _unitCounter++;
        private const int _maximumSelectionTargets = 3;

        private List<Vector2Int> _outOfRangeTargets = new(); // поле, со списком недосягаемых целей
        private static int _unitCounter = 0; 
        private int _unitId = _unitCounter++;
        private const int _maximumSelectionTargets = 3;

        protected override void GenerateProjectiles(Vector2Int forTarget, List<BaseProjectile> intoList)
        {
            float overheatTemperature = OverheatTemperature;

            if (GetTemperature() >= overheatTemperature) // проверка температуры
            {
                Debug.Log("перегрев");
                return;
            }

            int currentTemp = GetTemperature(); // сохранение текущей температуры

            for (int i = 0; i < currentTemp + 1; i++) // увеличение снарядов с каждым выстрелом
            {
                var projectile = CreateProjectile(forTarget);
                AddProjectileToList(projectile, intoList);
                Debug.Log($"Выстрел {i}, температура: {currentTemp}");
            }

            IncreaseTemperature(); // нагрев после выстрела

        }

        public override Vector2Int GetNextStep()
        {
            List<Vector2Int> inRangeTargets = SelectTargets();
            if (inRangeTargets.Count > 0) // Если есть цель, стреляем.
            {
                return unit.Pos;
            }

            if (_outOfRangeTargets.Count > 0) // Если цели нет
            {
                Vector2Int target = _outOfRangeTargets[0]; // Берём первую далёкую цель
                Vector2Int nextPosition = unit.Pos.CalcNextStepTowards(target); // Идем до далекой цели
                return nextPosition;
            }
            return unit.Pos;
        }

        protected override List<Vector2Int> SelectTargets()
        {
            _outOfRangeTargets.Clear();

            List<Vector2Int> allTargets = GetAllTargets().ToList();// Получение всех возможных целей
            List<Vector2Int> result = new List<Vector2Int>(); // Получение доступных целей

            if (allTargets.Count == 0) // Если в списке нет целей, то добавляем в список базу противника
            {
                Vector2Int enemyBase = runtimeModel.RoMap.Bases[IsPlayerUnitBrain ? RuntimeModel.BotPlayerId : RuntimeModel.PlayerId];
                allTargets.Add(enemyBase);
            }

            SortByDistanceToOwnBase(allTargets); // Производим сортировку целей по дистанции до базы

            int indexTarget = _unitId % _maximumSelectionTargets; // Определяем, какую именно по счёту ближайшую цель атакует юнит (0, 1, 2)

            if (indexTarget >= allTargets.Count) // Если целей больше, выбираем последнюю в списке
            {
                indexTarget = allTargets.Count - 1;
            }

            Vector2Int selectedTarget = allTargets[indexTarget]; // выбираем цель

            (IsTargetInRange(selectedTarget) // Если цель в радиусе актаки, 
                ? result // То добавляем в список доступных целей
                : _outOfRangeTargets // Иначе в список недосягаемых
            ).Add(selectedTarget);

            return result;
        }


        public override void Update(float deltaTime, float time)
        {
            if (_overheated)
            {
                _cooldownTime += Time.deltaTime;
                float t = _cooldownTime / (OverheatCooldown / 10);
                _temperature = Mathf.Lerp(OverheatTemperature, 0, t);
                if (t >= 1)
                {
                    _cooldownTime = 0;
                    _overheated = false;
                }
            }
        }

        private int GetTemperature()
        {
            if (_overheated) return (int)OverheatTemperature;
            else return (int)_temperature;
        }

        private void IncreaseTemperature()
        {
            _temperature += 1f;
            if (_temperature >= OverheatTemperature) _overheated = true;
        }
    }
}