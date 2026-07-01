import json
from pathlib import Path

from app.backend_server import HyperBoostBackendServer


REPO_ROOT = Path(__file__).resolve().parents[1]
ACTION_MAP = REPO_ROOT / "wpf" / "Data" / "ui_action_map_v2_10.json"
ACTION_DOC = REPO_ROOT / "docs" / "UI_ACTION_MAP_v2.10.0.md"
VERSION_FILE = REPO_ROOT / "VERSION"
NAVIGATION_VM = REPO_ROOT / "wpf" / "ViewModels" / "MainWindowViewModel.cs"
MAIN_WINDOW = REPO_ROOT / "wpf" / "MainWindow.xaml.cs"


def _load_map():
    return json.loads(ACTION_MAP.read_text(encoding="utf-8-sig"))


def _expected_version():
    return VERSION_FILE.read_text(encoding="utf-8").strip()


def _route_lookup(app):
    lookup = {}
    for rule in app.url_map.iter_rules():
        methods = set(rule.methods or set()) - {"HEAD", "OPTIONS"}
        lookup.setdefault(rule.rule, set()).update(methods)
    return lookup


def _path_without_query(path):
    return path.split("?", 1)[0]


def test_ui_action_map_button_density_and_states():
    payload = _load_map()
    menus = payload["menus"]
    expected_version = _expected_version()
    assert payload["app_version"] == expected_version
    assert payload["channel"] == ("Beta" if "-" in expected_version else "Stable")
    assert len(menus) >= 60
    assert payload["summary"]["total_buttons"] >= 360
    assert payload["summary"]["total_active_buttons"] == payload["summary"]["total_buttons"]

    valid_status = {"Real"}
    total_buttons = 0
    assert payload["summary"]["total_partial_or_roadmap_buttons"] == 0
    for menu in menus:
        assert menu["status"] == "Real", menu["key"]
        actions = menu["actions"]
        total_buttons += len(actions)
        assert len(actions) >= 6, menu["key"]
        if menu["big"]:
            assert len(actions) >= 10, menu["key"]

        commands = {action["command"] for action in actions}
        assert len(commands) == len(actions), menu["key"]

        for action in actions:
            assert action["label"].strip(), (menu["key"], action)
            assert action["command"].endswith("Command"), (menu["key"], action)
            assert action["method"] in {"GET", "POST", "PUT", "PATCH", "DELETE"}
            assert action["path"].startswith("/api/")
            assert action["status"] in valid_status, (menu["key"], action["status"])
            assert action["loading_state"]
            assert action["success_state"]
            assert action["error_state"]
            assert action["test_coverage"] == "tests/test_ui_action_map_v210.py"

            if action["method"] != "GET":
                assert action["safety_guard"] is True, (menu["key"], action)
                assert action["preview_required"] is True or ".preview." in action["id"], (menu["key"], action)

            if ".apply." in action["id"] or ".restore." in action["id"] or action["is_destructive"]:
                assert action["safety_guard"] is True, (menu["key"], action)
                assert action["restore"] is True, (menu["key"], action)

            if action["is_destructive"]:
                assert action["preview_required"] is True, (menu["key"], action)
                assert action["confirmation_required"] is True, (menu["key"], action)

    assert total_buttons == payload["summary"]["total_buttons"]


def test_ui_action_map_routes_are_registered(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    monkeypatch.delenv("HYPERBOOSTX_SESSION_TOKEN", raising=False)

    server = HyperBoostBackendServer()
    routes = _route_lookup(server.app)
    missing = []

    for menu in _load_map()["menus"]:
        for action in menu["actions"]:
            path = _path_without_query(action["path"])
            methods = routes.get(path)
            if methods is None or action["method"] not in methods:
                missing.append(f"{menu['key']}::{action['label']} {action['method']} {path}")

    assert not missing, "\n".join(missing)


def test_ui_action_map_is_visible_in_wpf_navigation_and_docs():
    payload = _load_map()
    navigation_source = NAVIGATION_VM.read_text(encoding="utf-8")
    route_source = MAIN_WINDOW.read_text(encoding="utf-8")
    docs = ACTION_DOC.read_text(encoding="utf-8-sig")

    assert "UI Action Map v2.10.0" in docs
    assert f"| Total menus | {payload['summary']['total_menus']} |" in docs
    assert f"| Total buttons | {payload['summary']['total_buttons']} |" in docs

    hidden_keys = {"Default"}
    missing_nav = []
    missing_route = []
    for menu in payload["menus"]:
        key = menu["key"]
        if key in hidden_keys:
            continue
        if f'Key = "{key}"' not in navigation_source:
            missing_nav.append(key)
        route_registered = f'_navigationService.Register("{key}"' in route_source or f'RegisterLegacyRoute("{key}"' in route_source
        if not route_registered:
            missing_route.append(key)

    assert not missing_nav, missing_nav
    assert not missing_route, missing_route


def test_stable_mode_feature_registry_exposes_only_real_features(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    monkeypatch.setenv("HYPERBOOSTX_MODE", "stable")
    monkeypatch.setenv("HYPERBOOSTX_SHOW_EXPERIMENTAL", "false")
    monkeypatch.setenv("HYPERBOOSTX_REQUIRE_REAL_FEATURES", "true")
    monkeypatch.setenv("HYPERBOOSTX_BLOCK_NON_REAL_STABLE_UI", "true")
    monkeypatch.delenv("HYPERBOOSTX_SESSION_TOKEN", raising=False)

    server = HyperBoostBackendServer()
    client = server.app.test_client()

    audit = client.get("/api/features/audit")
    payload = audit.get_json()
    expected_total = _load_map()["summary"]["total_menus"]
    assert audit.status_code == 200
    assert payload["ok"] is True
    assert payload["mode"] == "stable"
    assert payload["counts"]["total_original_features"] == expected_total
    assert payload["counts"]["stable_visible_features"] == expected_total
    assert payload["counts"]["non_real_visible_in_stable"] == 0
    assert payload["counts"]["hidden_from_stable"] == 0

    stable = client.get("/api/features/stable-visible").get_json()
    non_real = client.get("/api/features/non-real").get_json()
    stable_keys = {item["key"] for item in stable["items"]}
    non_real_keys = {item["key"] for item in non_real["items"]}

    assert "Dashboard" in stable_keys
    assert "OneClickBoost" in stable_keys
    assert "PluginMarketplace" in stable_keys
    assert "CloudSyncLicense" in stable_keys
    assert "RgbSoftwareDetector" in stable_keys
    assert "SystemRealityGuard" in stable_keys
    assert "LcdPerformanceGuard" in stable_keys
    assert "CpuTurboDiagnostic" in stable_keys
    assert "DefenderScanGuard" in stable_keys
    assert non_real["count"] == 0
    assert not non_real_keys
