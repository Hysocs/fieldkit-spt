namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void AppendExtractionEsp(
            Vector3 localPosition,
            ref int labelIndex)
        {
            if (_showExtractions == null ||
                !_showExtractions.Value ||
                _world == null ||
                _localPlayer == null ||
                _camera == null)
                return;

            RefreshExtractionPoints();

            float maxDistanceSq =
                _maxDistance.Value * _maxDistance.Value;
            for (int i = 0; i < _extractionPoints.Count; i++)
            {
                ExfiltrationPoint point = _extractionPoints[i];
                if (point == null)
                    continue;

                Vector3 position = point.transform.position;
                float distanceSq =
                    (position - localPosition).sqrMagnitude;
                if (distanceSq > maxDistanceSq)
                    continue;

                Vector3 screen =
                    _camera.WorldToScreenPoint(position);
                Vector2 canvasPosition;
                if (screen.z <= 0f ||
                    !TryScreenPointToCanvas(
                        _canvasRect,
                        new Vector2(screen.x, screen.y),
                        out canvasPosition))
                    continue;

                bool usable = _usableExtractionIds.Contains(
                    point.GetInstanceID());
                Color color = GetExtractionColor(usable);
                Text label = GetLabel(labelIndex++);
                RectTransform labelRect =
                    (RectTransform)label.transform;

                label.text =
                    "[EXTRACT] " + GetExtractionName(point) +
                    " | " + Mathf.Sqrt(distanceSq).ToString("0") + "m" +
                    (usable ? " | USABLE" : "");
                label.color = color;
                label.fontSize = _fontSize.Value;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = canvasPosition;
                label.gameObject.SetActive(true);
            }
        }

        private void RefreshExtractionPoints()
        {
            if (Time.unscaledTime < _nextExtractionRefresh)
                return;

            _nextExtractionRefresh = Time.unscaledTime + 1f;
            _extractionPoints.Clear();
            _usableExtractionIds.Clear();

            CommonAssets.Scripts.Game.ExfiltrationController controller =
                _world.ExfiltrationController;
            if (controller == null)
                return;

            AddExtractionPoints(controller.ExfiltrationPoints);
            AddExtractionPoints(controller.ScavExfiltrationPoints);

            try
            {
                ExfiltrationPoint[] usable =
                    controller.EligiblePoints(_localPlayer.Profile);
                if (usable == null)
                    return;

                for (int i = 0; i < usable.Length; i++)
                {
                    if (usable[i] != null)
                        _usableExtractionIds.Add(
                            usable[i].GetInstanceID());
                }
            }
            catch (Exception exception)
            {
                LogSource.LogDebug(
                    "Extraction eligibility refresh failed: " +
                    exception.Message);
            }
        }

        private void AddExtractionPoints(
            ExfiltrationPoint[] points)
        {
            if (points == null)
                return;

            for (int i = 0; i < points.Length; i++)
            {
                ExfiltrationPoint point = points[i];
                if (point == null)
                    continue;

                int id = point.GetInstanceID();
                bool duplicate = false;
                for (int j = 0; j < _extractionPoints.Count; j++)
                {
                    if (_extractionPoints[j] != null &&
                        _extractionPoints[j].GetInstanceID() == id)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                    _extractionPoints.Add(point);
            }
        }

        private static string GetExtractionName(
            ExfiltrationPoint point)
        {
            try
            {
                if (point.Settings != null &&
                    !string.IsNullOrEmpty(point.Settings.Name))
                    return point.Settings.Name;

                if (!string.IsNullOrEmpty(point.Description))
                    return point.Description;
            }
            catch { }

            return "Extraction";
        }
    }
}
