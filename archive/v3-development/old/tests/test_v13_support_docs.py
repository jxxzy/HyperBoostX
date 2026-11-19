from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def read_text(relative_path: str) -> str:
    return (ROOT / relative_path).read_text(encoding="utf-8")


def test_support_docs_faq_and_roadmap_exist_with_required_sections():
    required_files = [
        "docs/SUPPORT.md",
        "docs/templates/BUG_REPORT_TEMPLATE.md",
        "docs/templates/FEATURE_REQUEST_TEMPLATE.md",
        "docs/TROUBLESHOOTING.md",
        "docs/FAQ.md",
        "docs/ROADMAP.md",
    ]

    for relative_path in required_files:
        assert (ROOT / relative_path).exists(), relative_path

    support = read_text("docs/SUPPORT.md")
    faq = read_text("docs/FAQ.md")
    roadmap = read_text("docs/ROADMAP.md")
    troubleshooting = read_text("docs/TROUBLESHOOTING.md")

    for field in ["Version:", "Windows:", "CPU:", "RAM:", "GPU:", "Issue:", "Steps before error:", "Screenshot:", "Logs if available:"]:
        assert field in support

    assert "HyperBoostX does not guarantee FPS increase on every PC" in faq
    assert "does not force-disable Windows Defender" in faq
    assert "AMD Radeon" in faq
    assert "Intel Arc" in faq
    assert "Unknown Publisher" in faq
    assert "Safe Mode / Recovery Mode" in roadmap
    assert "App Integrity Check" in roadmap
    assert "local crash report export with redaction" in roadmap
    assert "License activation is not implemented" in roadmap
    assert "v2.10.0" in roadmap
    assert "not uploaded automatically" in troubleshooting


def test_api_reference_documents_v14_backend_contracts():
    api_reference = read_text("docs/API_REFERENCE.md")
    backend_sources = "\n".join([
        read_text("app/api/hardware.py"),
        read_text("app/api/boost.py"),
        read_text("app/api/jobs.py"),
        read_text("app/api/reports.py"),
        read_text("app/api/system_info.py"),
        read_text("app/api/health.py"),
        read_text("app/api/product_v14.py"),
    ])

    required_endpoints = [
        "/api/health",
        "/api/version",
        "/api/system/stats",
        "/api/system/info",
        "/api/system/startup",
        "/api/system/processes",
        "/api/hardware/profile",
        "/api/hardware/gpu",
        "/api/hardware/vendors",
        "/api/hardware/overlays",
        "/api/boost/plan",
        "/api/boost/apply",
        "/api/boost/undo",
        "/api/reports/latest",
        "/api/reports/export",
        "/api/reports/crash-export",
        "/api/jobs/start",
        "/api/jobs/{id}",
        "/api/jobs/{id}/cancel",
        "/api/advisor/performance",
        "/api/knowledge/terms",
        "/api/score/engine",
        "/api/games/library",
        "/api/overlays/status",
        "/api/protection/processes",
        "/api/benchmark/history",
        "/api/gpu/vendor-guide",
        "/api/drivers/recommendation",
        "/api/product/v2-roadmap",
    ]

    for endpoint in required_endpoints:
        assert endpoint in api_reference, endpoint

    for route_fragment in ["/gpu", "/vendors", "/overlays", "/profile", "/plan", "/apply", "/undo", "/crash-export"]:
        assert route_fragment in backend_sources, route_fragment

    assert "X-HyperBoostX-Session" in api_reference
    assert "privacy redaction" in api_reference
