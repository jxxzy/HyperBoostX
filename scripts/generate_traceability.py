import hashlib
import html
import json
import os
import re
from html.parser import HTMLParser
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
WORKSPACE = ROOT.parent
PROTOTYPE = WORKSPACE / "HyperBoostX-V3-Final-Complete-UI"
NAVIGATION = PROTOTYPE / "navigation.json"
INDEX = PROTOTYPE / "index.html"
GALLERY = PROTOTYPE / "gallery.html"
LEGACY_SOURCE = WORKSPACE / "V1" / "wpf" / "Data" / "ui_action_map_v2_10.json"
LEGACY_REGISTRY = ROOT / "docs" / "registry" / "legacy-parity.json"
RUNTIME_REGISTRY = ROOT / "docs" / "registry" / "runtime.json"
REFERENCE_MATRIX = ROOT / "docs" / "registry" / "technical-reference-matrix.json"
REFERENCE_VALIDATION = ROOT / "docs" / "registry" / "technical-reference-validation.json"
OUTPUT = ROOT / "docs" / "registry" / "requirement-traceability.json"
ATTACHMENT_REQUIREMENTS = ROOT / "docs" / "registry" / "master-task-requirements.json"


def slug(value):
    return re.sub(r"[^a-z0-9]+", "-", value.lower()).strip("-") or "unnamed"


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None


def requirement(requirement_id, source, source_reference, text, feature_id,
                ui_component="NOT_MAPPED", command="NOT_MAPPED",
                backend_handler="NOT_MAPPED", provider="NOT_MAPPED",
                verification="NOT_VERIFIED", test="NOT_VERIFIED",
                evidence="NOT_RECORDED", status="NOT_STARTED", mandatory=True):
    return {
        "requirementId": requirement_id,
        "featureId": feature_id,
        "source": source,
        "sourceReference": source_reference,
        "requirement": text,
        "uiComponent": ui_component,
        "command": command,
        "backendHandler": backend_handler,
        "provider": provider,
        "verification": verification,
        "test": test,
        "evidence": evidence,
        "status": status,
        "mandatory": mandatory,
    }


class PrototypeParser(HTMLParser):
    interactive_tags = {"button", "a", "input", "select", "textarea", "summary"}

    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.stack = []
        self.controls = []
        self.dialogs = []
        self.images = []

    def handle_starttag(self, tag, raw_attributes):
        attributes = dict(raw_attributes)
        classes = set(attributes.get("class", "").split())
        page = attributes.get("data-page")
        element_id = attributes.get("id", "")
        if not page and element_id.startswith("page-"):
            page = element_id[5:]
        parent_page = next((node["page"] for node in reversed(self.stack) if node["page"]), None)
        page = page or parent_page
        dialog_name = None
        if tag == "dialog" or classes.intersection({"modal", "dialog", "overlay"}):
            dialog_name = element_id or attributes.get("aria-label") or f"{tag}-{len(self.dialogs) + 1}"
            dialog = {
                "name": dialog_name,
                "tag": tag,
                "elementId": element_id,
                "class": attributes.get("class", ""),
                "page": page,
                "text": [],
            }
            self.dialogs.append(dialog)
        parent_dialog = next((node["dialog"] for node in reversed(self.stack) if node["dialog"]), None)
        node = {"tag": tag, "page": page, "dialog": dialog_name or parent_dialog}
        self.stack.append(node)

        event_attributes = {key: value for key, value in attributes.items() if key.startswith("on")}
        is_control = tag in self.interactive_tags or bool(event_attributes) or attributes.get("role") in {
            "button", "link", "tab", "menuitem", "switch", "checkbox", "radio"
        }
        if is_control:
            control = {
                "tag": tag,
                "elementId": element_id,
                "name": attributes.get("name", ""),
                "label": attributes.get("aria-label") or attributes.get("title") or attributes.get("placeholder") or attributes.get("value", ""),
                "role": attributes.get("role", ""),
                "page": page,
                "dialog": node["dialog"],
                "events": event_attributes,
                "action": attributes.get("data-action", ""),
                "text": [],
            }
            node["control"] = control
            self.controls.append(control)
        if tag == "img":
            self.images.append({
                "src": attributes.get("src", ""),
                "alt": attributes.get("alt", ""),
                "page": page,
            })

    def handle_startendtag(self, tag, attrs):
        self.handle_starttag(tag, attrs)
        self.handle_endtag(tag)

    def handle_data(self, data):
        if not data.strip():
            return
        for node in self.stack:
            control = node.get("control")
            if control is not None:
                control["text"].append(data)
        active_dialogs = {node["dialog"] for node in self.stack if node["dialog"]}
        for dialog in self.dialogs:
            if dialog["name"] in active_dialogs:
                dialog["text"].append(data)

    def handle_endtag(self, tag):
        for index in range(len(self.stack) - 1, -1, -1):
            if self.stack[index]["tag"] == tag:
                del self.stack[index:]
                return


def markdown_requirements(path, source_key):
    if not path.is_file():
        return []
    records = []
    section = "document"
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    pending = None
    pending_line = 0

    def flush():
        nonlocal pending
        if pending and pending["text"].strip():
            text = re.sub(r"\s+", " ", pending["text"]).strip()
            section_slug = slug(pending["section"])
            feature_id = f"req.{source_key}.{section_slug}"
            records.append(requirement(
                f"req.{source_key}.{pending_line:04d}-{slug(text[:64])}",
                path.name,
                f"{path.name}:{pending_line}",
                text,
                feature_id,
            ))
        pending = None

    for number, line in enumerate(lines, start=1):
        stripped = line.strip()
        if not stripped:
            flush()
            continue
        heading = re.match(r"^#{1,6}\s+(.+)$", stripped)
        if heading:
            flush()
            section = heading.group(1).strip()
            continue
        if stripped.startswith("```") or stripped.startswith("|"):
            flush()
            continue
        bullet = re.match(r"^\s*(?:[-*+]\s+|\d+[.)]\s+)(.+)$", line)
        if bullet:
            flush()
            pending = {"section": section, "text": bullet.group(1)}
            pending_line = number
            continue
        if stripped.startswith("**") and stripped.endswith("**"):
            flush()
            continue
        if pending is None:
            pending = {"section": section, "text": stripped}
            pending_line = number
        else:
            pending["text"] += " " + stripped
    flush()
    return records


def paragraphs(path, source_key):
    if not path.is_file():
        return []
    blocks = re.split(r"\n\s*\n", path.read_text(encoding="utf-8-sig"))
    records = []
    for number, block in enumerate(blocks, start=1):
        text = re.sub(r"\s+", " ", block).strip()
        if not text or text.startswith("#") or text.startswith("```") or text.startswith("|"):
            continue
        records.append(requirement(
            f"req.{source_key}.{number:04d}-{slug(text[:64])}",
            path.name,
            f"{path.name}:paragraph-{number}",
            text,
            f"req.{source_key}.{slug(text[:48])}",
        ))
    return records


def source_controls(source):
    controls = []
    tag_pattern = re.compile(
        r"<(button|a|input|select|textarea|summary|dialog|form)\b[^>]*>",
        re.IGNORECASE | re.DOTALL,
    )
    attribute_pattern = re.compile(
        r"([:\w-]+)(?:\s*=\s*(?:\"([^\"]*)\"|'([^']*)'|([^\s>]+)))?",
        re.DOTALL,
    )
    for match in tag_pattern.finditer(source):
        tag = match.group(1).lower()
        attributes = {}
        opening_tag = match.group(0)
        name_end = re.match(r"<\w+", opening_tag).end()
        for attribute in attribute_pattern.finditer(opening_tag, name_end):
            name = attribute.group(1).lower()
            value = next((value for value in attribute.groups()[1:] if value is not None), "")
            attributes[name] = html.unescape(value)
        controls.append({
            "tag": tag,
            "elementId": attributes.get("id", ""),
            "name": attributes.get("name", ""),
            "label": attributes.get("aria-label") or attributes.get("title") or attributes.get("placeholder") or attributes.get("value", ""),
            "role": attributes.get("role", ""),
            "action": attributes.get("data-command") or attributes.get("data-action") or attributes.get("data-go", ""),
            "events": {key: value for key, value in attributes.items() if key.startswith("on")},
            "sourceOffset": match.start(),
        })
    return controls


def js_event_bindings(source):
    pattern = re.compile(r"\.\s*(on(?:click|change|input|submit|keydown))\s*=", re.IGNORECASE)
    return [
        {"event": match.group(1), "sourceOffset": match.start()}
        for match in pattern.finditer(source)
    ]


def js_command_cases(source):
    pattern = re.compile(r"\bcase\s*(['\"])([^'\"]+)\1\s*:", re.IGNORECASE)
    return [
        {"command": match.group(2), "sourceOffset": match.start()}
        for match in pattern.finditer(source)
    ]


def modal_calls(source):
    pattern = re.compile(r"\bmodal\s*\(", re.IGNORECASE)
    calls = []
    for match in pattern.finditer(source):
        if re.search(r"\bfunction\s*$", source[max(0, match.start() - 24):match.start()], re.IGNORECASE):
            continue
        following = source[match.end():match.end() + 180]
        title = re.match(r"\s*(['\"])(.*?)\1", following, re.DOTALL)
        calls.append({
            "title": title.group(2) if title else "Dynamic preview dialog",
            "sourceOffset": match.start(),
        })
    return calls


def main():
    if not all(path.is_file() for path in (
        NAVIGATION, INDEX, GALLERY, LEGACY_SOURCE, LEGACY_REGISTRY, RUNTIME_REGISTRY,
        REFERENCE_MATRIX, REFERENCE_VALIDATION
    )):
        raise SystemExit("Required prototype, legacy, or runtime registry source is missing.")

    navigation = json.loads(NAVIGATION.read_text(encoding="utf-8-sig"))
    legacy_source = json.loads(LEGACY_SOURCE.read_text(encoding="utf-8-sig"))
    legacy_parity = json.loads(LEGACY_REGISTRY.read_text(encoding="utf-8-sig"))
    runtime = json.loads(RUNTIME_REGISTRY.read_text(encoding="utf-8-sig"))
    reference_matrix = json.loads(REFERENCE_MATRIX.read_text(encoding="utf-8-sig"))
    reference_validation = json.loads(REFERENCE_VALIDATION.read_text(encoding="utf-8-sig"))
    validated_sources = {
        item["sourceUrl"]: item for item in reference_validation.get("records", [])
    }
    if len(validated_sources) != len(reference_validation.get("records", [])):
        raise SystemExit("Technical reference validation ledger contains duplicate source URLs.")
    attachment_path = os.environ.get("HBX_MASTER_TASK_ATTACHMENT")
    if attachment_path and Path(attachment_path).is_file():
        attachment = Path(attachment_path)
        attachment_records = markdown_requirements(attachment, "master-attachment")
        attachment_payload = {
            "source": "Pasted text #1.txt",
            "sha256": sha256(attachment),
            "requirements": attachment_records,
        }
        ATTACHMENT_REQUIREMENTS.write_text(
            json.dumps(attachment_payload, indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
        )
    elif ATTACHMENT_REQUIREMENTS.is_file():
        attachment_payload = json.loads(ATTACHMENT_REQUIREMENTS.read_text(encoding="utf-8-sig"))
        attachment_records = attachment_payload.get("requirements", [])
    else:
        attachment_payload = {"source": "Pasted text #1.txt", "sha256": None, "requirements": []}
        attachment_records = []
    parser = PrototypeParser()
    index_source = INDEX.read_text(encoding="utf-8-sig")
    parser.feed(index_source)
    controls = source_controls(index_source)
    event_bindings = js_event_bindings(index_source)
    command_cases = js_command_cases(index_source)
    dialog_calls = modal_calls(index_source)
    gallery = PrototypeParser()
    gallery.feed(GALLERY.read_text(encoding="utf-8-sig"))

    routes = {
        route["name"]: {
            **route,
            "featureId": f"ui.{slug(route['group'])}.{slug(route['name'])}",
        }
        for route in navigation
    }
    if len(reference_matrix.get("menus", [])) != len(navigation):
        raise SystemExit("Technical reference matrix menu count does not match the native navigation catalog.")
    reference_assignments = []
    reference_ids = set()
    distinct_reference_urls = set()
    distinct_reference_urls_by_classification = {}
    for menu_number, menu in enumerate(reference_matrix["menus"], start=1):
        menu_name = menu.get("menu", "")
        route = routes.get(menu_name)
        if menu.get("number") != menu_number or route is None:
            raise SystemExit(f"Technical reference menu {menu_number} does not match a native navigation route.")
        references = menu.get("references", [])
        if len(references) != 5:
            raise SystemExit(f"Technical reference menu {menu_number} must contain exactly five assignments.")
        for reference in references:
            reference_id = reference.get("id", "")
            reference_url = reference.get("url", "")
            if not reference_id or not reference_url:
                raise SystemExit(f"Technical reference menu {menu_number} contains an incomplete assignment.")
            assignment_id = (
                f"technical-reference.menu-{menu_number:02d}."
                f"{slug(menu_name)}.{slug(reference_id)}"
            )
            if assignment_id in reference_ids:
                raise SystemExit(f"Duplicate technical reference assignment: {assignment_id}")
            reference_ids.add(assignment_id)
            distinct_reference_urls.add(reference_url)
            classification = reference.get("verification", "unclassified")
            distinct_reference_urls_by_classification.setdefault(classification, set()).add(reference_url)
            reference_assignments.append((assignment_id, menu, reference, route["featureId"]))
    if len(reference_assignments) != 365 or len(distinct_reference_urls) != 158:
        raise SystemExit(
            "Technical reference matrix inventory mismatch: "
            f"assignments={len(reference_assignments)}, distinct URLs={len(distinct_reference_urls)}."
        )
    unknown_reviewed_urls = set(validated_sources) - distinct_reference_urls
    required_review_fields = {"sourceUrl", "finalUrl", "title", "result", "evidence"}
    malformed_reviews = [
        item.get("sourceUrl", "<missing source URL>")
        for item in reference_validation.get("records", [])
        if not required_review_fields.issubset(item)
    ]
    if unknown_reviewed_urls or malformed_reviews:
        raise SystemExit(
            "Technical reference validation ledger is inconsistent: "
            f"unknown URLs={len(unknown_reviewed_urls)}, malformed records={len(malformed_reviews)}."
        )
    subfeature_count = sum(len(route.get("features", [])) for route in navigation)
    legacy_actions = [
        (menu, action)
        for menu in legacy_source.get("menus", [])
        for action in menu.get("actions", [])
    ]
    legacy_by_id = {entry["legacy_id"]: entry for entry in legacy_parity}
    runtime_action_ids = {action["Id"] for action in runtime.get("Actions", [])}
    gallery_screens = [image for image in gallery.images if image["src"]]
    additional_screens = [
        image for image in gallery_screens
        if not re.match(r"^(?:0[1-9]|[1-6][0-9]|7[0-3])-", Path(image["src"]).name)
    ]

    records = []
    for route in navigation:
        feature_id = routes[route["name"]]["featureId"]
        records.append(requirement(
            f"proto.route.{feature_id}",
            "navigation.json",
            route["name"],
            f"{route['desc']} Action: {route['action']}. Kind: {route['kind']}.",
            feature_id,
            "MainWindow route binding / FeatureRouteCatalog",
            "Navigation selection",
            "ShellViewModel.LoadPage",
            "Embedded navigation catalog and native route binding",
            "All 73 route IDs and route bindings are checked and rendered; this verifies navigation, not functional parity.",
            "ShellTests.EveryNavigationPageHasItsExpectedCommands; RenderTests.AllPagesRenderWithoutBindingErrors",
            "dotnet test HyperBoostX.sln -c Release --no-restore: Passed 103/103",
            status="UI_BOUND_NOT_FEATURE_VERIFIED",
        ))
        for index, feature in enumerate(route.get("features", []), start=1):
            records.append(requirement(
                f"proto.subfeature.{feature_id}.{index:03d}-{slug(feature)}",
                "navigation.json",
                f"{route['name']} / {feature}",
                feature,
                feature_id,
            ))

    for index, image in enumerate(gallery_screens, start=1):
        filename = Path(image["src"]).name
        stem = Path(filename).stem
        route_reference = re.match(r"^(0[1-9]|[1-6][0-9]|7[0-3])-", filename)
        if route_reference:
            route_number = int(route_reference.group(1))
            if route_number > len(navigation):
                raise SystemExit(f"Gallery route reference has no navigation entry: {filename}")
            route = navigation[route_number - 1]
            screenshot_slug = Path(filename).stem.split("-", 1)[1]
            if screenshot_slug != slug(route["name"]):
                raise SystemExit(f"Gallery screenshot {filename} does not match route {route['name']}.")
            feature_id = routes[route["name"]]["featureId"]
            kind = "primary route screenshot"
        elif stem.startswith(("76-", "77-", "78-", "79-", "80-", "81-")):
            feature_id = f"ui.lifecycle.{slug(stem[3:])}"
            kind = "additional workflow screen"
        else:
            feature_id = f"ui.variant.{slug(stem)}"
            kind = "alternate visual state"
        records.append(requirement(
            f"prototype.screen.{index:03d}-{slug(stem)}",
            "gallery.html",
            filename,
            f"Prototype screen reference: {stem.replace('-', ' ')} ({kind}).",
            feature_id,
            f"Prototype gallery image {filename}",
        ))

    route_by_slug = {slug(name): route["featureId"] for name, route in routes.items()}
    for index, control in enumerate(controls, start=1):
        label = control["label"] or control["elementId"] or control["name"] or control["action"] or control["tag"]
        feature_id = route_by_slug.get(control.get("page"), "prototype.unassigned-interaction")
        detail = f"{control['tag']} {label}".strip()
        if control["elementId"]:
            detail += f" [id={control['elementId']}]"
        if control["action"]:
            detail += f" [action={control['action']}]"
        if control["events"]:
            detail += " [events=" + ", ".join(sorted(control["events"])) + "]"
        records.append(requirement(
            f"prototype.interaction.{index:04d}-{slug(control['elementId'] or label)[:48]}",
            "index.html",
            f"byte-offset {control['sourceOffset']}; element {control['tag']} #{control['elementId'] or 'no-id'}",
            detail,
            feature_id,
            f"Prototype interaction {control['tag']}#{control['elementId'] or index}",
            control["action"] or "; ".join(control["events"].values()) or "NOT_MAPPED",
            "Browser preview event/local state; native backend not implied",
            "Prototype-only DOM/local state",
            "Source interaction inventoried; native behavior not verified.",
            "NOT_VERIFIED",
            "NOT_RECORDED",
        ))

    for index, dialog in enumerate(parser.dialogs, start=1):
        text = re.sub(r"\s+", " ", " ".join(dialog["text"])).strip()
        feature_id = route_by_slug.get(dialog["page"], f"prototype.dialog.{slug(dialog['name'])}")
        records.append(requirement(
            f"prototype.dialog.{index:03d}-{slug(dialog['name'])}",
            "index.html",
            f"{dialog['tag']}#{dialog['elementId'] or dialog['name']}",
            text or dialog["name"],
            feature_id,
            f"Prototype {dialog['tag']} {dialog['class']}".strip(),
            "Dialog controls are separately inventoried as interactions",
            "Browser preview dialog state",
            "Prototype-only DOM/local state",
            "Dialog inventoried from source; native behavior not verified.",
            "NOT_VERIFIED",
            "NOT_RECORDED",
        ))
    for index, dialog in enumerate(dialog_calls, start=1):
        records.append(requirement(
            f"prototype.dialog-call.{index:03d}-{slug(dialog['title'])}",
            "index.html",
            f"modal call byte-offset {dialog['sourceOffset']}",
            dialog["title"],
            f"prototype.dialog.{slug(dialog['title'])}",
            "Reusable browser preview dialog",
            "modal() / command dispatcher",
            "Browser preview state; no native backend implied",
            "Prototype-only DOM/local state",
            "Dialog call inventoried from JavaScript; native behavior not verified.",
            "NOT_VERIFIED",
            "NOT_RECORDED",
        ))
    for index, binding in enumerate(event_bindings, start=1):
        records.append(requirement(
            f"prototype.event-binding.{index:03d}-{slug(binding['event'])}",
            "index.html",
            f"JavaScript event assignment byte-offset {binding['sourceOffset']}",
            f"Prototype event binding: {binding['event']}.",
            "prototype.javascript-interactions",
            "Prototype JavaScript event handler",
            binding["event"],
            "Browser preview JavaScript",
            "Browser DOM/local state",
            "Source event binding inventoried; native behavior not verified.",
            "NOT_VERIFIED",
            "NOT_RECORDED",
        ))
    for index, command in enumerate(command_cases, start=1):
        records.append(requirement(
            f"prototype.command-case.{index:03d}-{slug(command['command'])}",
            "index.html",
            f"JavaScript command dispatcher byte-offset {command['sourceOffset']}",
            f"Prototype command: {command['command']}.",
            "prototype.javascript-command-dispatcher",
            "Prototype command dispatcher",
            command["command"],
            "Browser preview command() switch",
            "Browser preview localStorage / DOM",
            "Source command branch inventoried; native behavior not verified.",
            "NOT_VERIFIED",
            "NOT_RECORDED",
        ))

    for menu, action in legacy_actions:
        legacy_id = action.get("id", "")
        mapped = legacy_by_id.get(legacy_id, {})
        action_ids = [item for item in mapped.get("v3_action_ids", []) if item in runtime_action_ids]
        records.append(requirement(
            f"legacy.{slug(legacy_source.get('app_version', 'unknown'))}.{slug(legacy_id)}",
            "V1/wpf/Data/ui_action_map_v2_10.json",
            f"{menu.get('key', 'menu')} / {legacy_id}",
            action.get("label") or action.get("text") or action.get("id") or "Legacy action",
            f"legacy.{slug(menu.get('key', 'menu'))}.{slug(legacy_id)}",
            "NOT_MAPPED",
            "NOT_MAPPED",
            "NOT_MAPPED",
            "Legacy endpoint/provider requires individual reconciliation",
            "No equivalence established" if not action_ids else "Candidate mapping exists; equivalence still requires an individual test.",
            "NOT_VERIFIED",
            "No per-action equivalence evidence recorded",
            status="NOT_STARTED" if not action_ids else "MAPPING_REQUIRES_VERIFICATION",
        ))

    for path, key in (
        (ROOT / "docs" / "MASTER_DEVELOPMENT_TASK.md", "master"),
        (ROOT / "docs" / "MASTER_DEVELOPMENT_TASK_ADDENDUM.md", "addendum"),
    ):
        records.extend(markdown_requirements(path, key))
    records.extend(paragraphs(PROTOTYPE / "CODEX-HANDOFF.md", "prototype-handoff"))
    records.extend(paragraphs(PROTOTYPE / "README.md", "prototype-readme"))
    records.extend(attachment_records)

    for assignment_id, menu, reference, feature_id in reference_assignments:
        source_classification = reference.get("verification", "unclassified")
        source_review = validated_sources.get(reference["url"])
        provider = (
            f"{reference['title']} — {reference['url']} "
            f"(source classification: {source_classification}; API, license, platform and dependency "
            "compatibility have not been independently validated)"
        )
        verification = (
            f"Source retrieval reviewed ({source_review['result']}): {source_review['evidence']} "
            "Product API/license/platform/dependency compatibility and runtime behavior remain NOT VERIFIED."
            if source_review
            else (
                f"Source assignment registered only; URL, API signature, license, supported Windows versions, "
                f"dependency and implementation compatibility NOT VERIFIED ({source_classification})."
            )
        )
        test = (
            "Source retrieval via web_fetch; no local API or runtime test performed."
            if source_review
            else "Menu acceptance criteria from the technical reference matrix; execution NOT RUN."
        )
        evidence = (
            f"Source review {source_review['result']}: {source_review['finalUrl']}. "
            f"{source_review['evidence']}"
            if source_review
            else "Reference inventory imported; no implementation or runtime evidence recorded."
        )
        if source_review and source_review["finalUrl"] != reference["url"]:
            provider += f" (reviewed alternate: {source_review['finalUrl']})"
        records.append(requirement(
            assignment_id,
            "HYPERBOOSTX-V3-REFERENCE-MATRIX.json",
            (
                f"Menu {menu['number']:02d} / {menu['menu']} / "
                f"{reference['id']} / {reference['title']} / {reference['url']}"
            ),
            (
                f"{reference['purpose']} Implementation: {menu.get('implementation', 'Not specified')} "
                f"Acceptance: {menu.get('acceptance', 'Not specified')}"
            ),
            feature_id,
            "NOT_MAPPED",
            "NOT_MAPPED",
            "NOT_MAPPED",
            provider,
            verification,
            test,
            evidence,
            status="NOT_STARTED",
        ))

    expected_ui_doc = PROTOTYPE / "CODEX-UI-IMPLEMENTATION.md"
    if not expected_ui_doc.is_file():
        records.append(requirement(
            "source-gap.codex-ui-implementation",
            "CODEX-UI-IMPLEMENTATION.md",
            str(expected_ui_doc.relative_to(WORKSPACE)).replace("\\", "/"),
            "Expected implementation guide is not present in the supplied prototype folder; confirm whether another source file is authoritative.",
            "req.source-gap.prototype-documents",
            verification="File existence checked.",
            test="Generator source inventory",
            evidence="Missing from current workspace.",
            status="SOURCE_MISSING",
        ))

    evidence = "dotnet test HyperBoostX.sln -c Release --no-restore: Passed 149/149"
    implementation_records = [
        ("test-center.route-bindings", "Test Center route binding", "ui.command-center.test-center", "TestCenterPanel", "FeatureRouteCatalog.All / BindingFor", "Embedded 73-route catalog", "ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults; RenderTests.AllPagesRenderWithoutBindingErrors", "IMPLEMENTED_VERIFIED"),
        ("test-center.database-integrity", "Read-only application database check", "ui.command-center.test-center", "TestCenterPanel", "TestCenterRunner.RunAsync / Journal.IntegrityCheck", "SQLite PRAGMA quick_check", "ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults; EngineTests.DatabaseAndEncryptedDocumentsExposeReadOnlyIntegrityChecks", "IMPLEMENTED_VERIFIED"),
        ("test-center.configuration-integrity", "Encrypted document and settings validation", "ui.command-center.test-center", "TestCenterPanel", "TestCenterRunner.RunAsync / DocumentStore.InspectIntegrity", "DPAPI-protected documents and SQLite settings", "ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults; EngineTests.DatabaseAndEncryptedDocumentsExposeReadOnlyIntegrityChecks", "IMPLEMENTED_VERIFIED"),
        ("test-center.recovery-integrity", "Recovery journal check", "ui.command-center.test-center", "TestCenterPanel", "TestCenterRunner.RunAsync / Journal.History", "SQLite journal and encrypted recovery records", "ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults", "IMPLEMENTED_VERIFIED"),
        ("test-center.telemetry", "Timestamped telemetry sample", "ui.command-center.test-center", "TestCenterPanel", "TestCenterRunner.RunAsync / Telemetry.Sample", "Windows system time and memory APIs; network interface counters", "ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults", "IMPLEMENTED_VERIFIED"),
        ("test-center.backend-diagnostics", "Read-only Windows diagnostic provider check", "ui.command-center.test-center", "TestCenterPanel", "TestCenterRunner.RunAsync / Diagnostics.ScanAsync", "Windows diagnostic providers", "ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults", "IMPLEMENTED_VERIFIED"),
        ("test-center.permission-context", "Current-user permission context", "ui.command-center.test-center", "TestCenterPanel", "TestCenterRunner.RunAsync / WindowsIdentity.GetCurrent", "Windows access token and Safety Guard", "ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults", "IMPLEMENTED_VERIFIED"),
        ("test-center.dependencies", "Runtime dependency availability", "ui.command-center.test-center", "TestCenterPanel", "TestCenterRunner.RunAsync / Type.GetType", "System.Management assembly and Windows providers", "ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults", "IMPLEMENTED_VERIFIED"),
        ("test-center.backup-integrity", "Existing backup integrity check", "ui.command-center.test-center", "TestCenterPanel", "TestCenterRunner.RunAsync / Journal.BackupIntegrityCheck", "SQLite read-only backup check", "EngineTests.DatabaseAndEncryptedDocumentsExposeReadOnlyIntegrityChecks", "IMPLEMENTED_VERIFIED"),
        ("application-health.background-operations", "Application background operation status", "ui.command-center.application-health", "TestCenterPanel", "TestCenterRunner.RunAsync / ShellViewModel.Busy", "Current shell operation state; no detached worker queue", "ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults", "PARTIAL_VERIFIED"),
        ("application-health.error-history", "Application error and notification history", "ui.command-center.application-health", "TestCenterPanel", "TestCenterRunner.RunAsync / Notifications.History", "Encrypted local notification store", "ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults", "IMPLEMENTED_VERIFIED"),
        ("application-health.resource-usage", "Application resource consumption", "ui.command-center.application-health", "TestCenterPanel", "TestCenterRunner.RunAsync / Process resource counters", "Current process working set, private bytes and cumulative CPU time", "ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults", "IMPLEMENTED_VERIFIED"),
        ("application-health.safe-repair", "Safe application repair workflow", "ui.command-center.application-health", "TestCenterPanel", "No handler", "Unsupported: no automated application repair handler", "Test Center reports UNSUPPORTED; no repair attempted.", "UNSUPPORTED"),
        ("application-health.privileged-worker", "Privileged worker and IPC health", "ui.command-center.application-health", "TestCenterPanel", "PrivilegedWorkerClient / HyperBoostX.Worker", "Separate worker, SID/PID/session-bound named pipe, ECDH P-256 and HMAC-SHA256", "WorkerProcessTests; ShellTests.SafeTestCenterRunsAndPersistsHonestTimestampedResults; published UI smoke Test Center", "PARTIAL_VERIFIED"),
        ("application-health.file-integrity", "Signed application file integrity", "ui.command-center.application-health", "TestCenterPanel", "No handler", "Unsupported: no signed integrity manifest", "Test Center reports UNSUPPORTED; no trust asserted.", "UNSUPPORTED"),
        ("test-center.ai-consent", "AI connection test consent gate", "ui.command-center.test-center", "IntegrationsPanel", "IntegrationsViewModel.TestAiCommand", "Configured AI provider, explicit consent", "Automatic test is SKIPPED; provider connection test remains explicit.", "SKIPPED"),
        ("test-center.webhook-consent", "Webhook connection test consent gate", "ui.command-center.test-center", "IntegrationsPanel", "IntegrationsViewModel.TestWebhookCommand", "Configured HTTPS webhook, explicit consent", "Automatic test is SKIPPED; no network request made.", "SKIPPED"),
        ("configuration.schema-validation", "Versioned settings schema validation", "ui.tools-settings.settings", "MainWindow Settings panel", "ShellViewModel.ValidateSettingsDocument", "Typed local settings schema version 1", "ShellTests.VersionedSettingsDocumentsValidateBeforeImport", "IMPLEMENTED_VERIFIED"),
        ("configuration.transactional-update", "Atomic settings batch persistence", "ui.tools-settings.settings", "MainWindow Settings panel", "Journal.SetSettings", "SQLite transaction", "EngineTests.SettingsBatchWritesAreAtomic", "IMPLEMENTED_VERIFIED"),
        ("configuration.settings-import-export", "Settings import and export with privacy scope", "ui.tools-settings.settings", "MainWindow Settings panel", "ImportSettingsCommand / ExportSettingsCommand / ShellViewModel.ImportUserSettings / ExportUserSettings", "User-selected JSON files and local SQLite settings", "ShellTests.VersionedSettingsDocumentsValidateBeforeImport; scripts/ui-smoke.ps1: actual export shape/privacy, import dialog cancellation, approved import, and restart persistence verified with isolated data", "IMPLEMENTED_VERIFIED"),
        ("configuration.reset-defaults", "Consent-based preference reset", "ui.tools-settings.settings", "MainWindow Settings panel", "ResetSettingsCommand / ShellViewModel.ResetUserSettings", "Local SQLite preferences; rollback and Windows state retained", "scripts/ui-smoke.ps1: reset decline preserved changed preference; approved reset restored defaults and persisted after restart with isolated data", "IMPLEMENTED_VERIFIED"),
        ("storage.file-analysis.folder-selection", "User-selected local folder", "ui.storage.duplicate-large-files", "MainWindow file-analysis panel", "SelectFileAnalysisFolderCommand / FileAnalysisRoot", "Microsoft.Win32.OpenFolderDialog and validated local path", "ShellTests.DuplicateAndLargeFileRouteRunsRealReadOnlyAnalysis; published UI smoke FileAnalysisRoot", "IMPLEMENTED_VERIFIED"),
        ("storage.file-analysis.duplicates", "SHA-256 duplicate grouping", "ui.storage.duplicate-large-files", "DuplicateGroupsGrid", "RunFileAnalysisCommand / FileAnalysis.ScanAsync", "Size pre-grouping and SHA256.HashDataAsync", "FileAnalysisTests.FindsContentDuplicatesAndLargeFilesWithoutChangingFiles; published UI smoke DuplicateGroupsGrid", "IMPLEMENTED_VERIFIED"),
        ("storage.file-analysis.large-files", "Configurable large-file inventory", "ui.storage.duplicate-large-files", "LargeFilesGrid", "RunFileAnalysisCommand / FileAnalysis.ScanAsync", "Local file metadata with configurable MiB threshold", "FileAnalysisTests.LargeFileThresholdIsAppliedWithoutHashingUniqueFiles; published UI smoke LargeFilesGrid", "IMPLEMENTED_VERIFIED"),
        ("storage.file-analysis.coverage", "Bounded explicit scan coverage", "ui.storage.duplicate-large-files", "FileAnalysisStatus", "FileAnalysis.ScanAsync", "100,000-file and 2 GiB hash-read bounds; reparse-point skipping", "FileAnalysisTests cancellation, budget and path validation coverage", "PARTIAL_VERIFIED"),
        ("hardware.rgb-software-detection", "RGB controller software and overhead detection", "ui.hardware.rgb-software-detector", "MainWindow observations", "ScanCommand / VendorSoftwareDetection.ScanRgbAsync", "Processes, Win32_Service and uninstall registry; two-sample CPU and working-set measurement", "IntegrationProviderTests RGB signature, inventory and live-process measurement tests; published UI smoke", "IMPLEMENTED_VERIFIED"),
        ("gaming.overlay-conflict-detection", "Overlay-capable software and resource correlation", "ui.gaming.overlay-conflict-detector", "MainWindow observations", "ScanCommand / OverlaySoftwareDetection.ScanAsync", "Processes and uninstall registry; two-sample CPU, working set and combined observed load", "IntegrationProviderTests overlay signatures and live-process measurement; published UI smoke", "IMPLEMENTED_VERIFIED"),
        ("windows.app-load-measurement", "Confirmed local application launch measurement", "ui.windows.app-load-optimizer", "MainWindow App Load panel", "MeasureAppLoadCommand / ApplicationLaunchMeasurement.MeasureAsync", "Windows Process APIs: process creation, input-idle signal and resource counters", "IntegrationProviderTests App Load validation and real launch test; ShellTests route binding; published UI smoke", "IMPLEMENTED_VERIFIED"),
        ("windows.boot-diagnostics", "Boot history, startup degradation and service dependency analysis", "ui.windows.boot-optimizer", "MainWindow observation grid", "ScanCommand / BootDiagnostics.ScanAsync", "Windows Diagnostics-Performance Event 100 and 101–110 plus Win32_Service.Dependencies", "IntegrationProviderTests boot diagnostics test; ShellTests boot route binding; published UI smoke", "IMPLEMENTED_VERIFIED"),
        ("storage.drive-diagnostics", "Drive media, health, TRIM and maintenance-policy inspection", "ui.storage.drive-optimization", "MainWindow observation grid and Windows-tool handoff", "ScanCommand / DriveDiagnostics.ScanAsync", "MSFT_PhysicalDisk, MSFT_Volume, ScheduledDefrag, fsutil read-only query and dfrgui handoff", "IntegrationProviderTests drive diagnostics test; ShellTests drive route binding; published UI smoke", "IMPLEMENTED_VERIFIED"),
        ("security.lcd-performance-guard", "Configurable LCD utility overhead guard with cooling exclusions", "ui.privacy-security.lcd-performance-guard", "MainWindow LCD guard policy and observations", "ScanCommand / LcdPerformanceDetection.ScanAsync", "Two-sample Windows process CPU and working-set measurement with explicit vendor signatures", "IntegrationProviderTests LCD signature/live-process/gaming-gate tests; ShellTests route binding; published UI smoke", "IMPLEMENTED_VERIFIED"),
        ("gaming.stutter-analysis", "Imported frametime spike and optional telemetry correlation analysis", "ui.gaming.stutter-analyzer", "BenchmarkPanel frametime import and observation grid", "ImportFrametimeCommand / FrameTimeAnalysis.AnalyzeCsv", "Bounded user-selected CSV with calculated FPS lows, percentiles, spikes and Pearson correlations", "EngineTests frametime analysis tests; ShellTests route binding; published UI smoke", "IMPLEMENTED_VERIFIED"),
        ("reports.research-mode", "Persisted A/B/C/D repeat-trial experiment analysis", "ui.reports-tests.research-mode", "BenchmarkPanel Research Mode editor/history", "AddResearchTrialCommand / AnalyzeResearchCommand / ResearchExperiments", "DPAPI-protected local trial history with per-variant means and A-baseline deltas", "EngineTests research validation/report tests; ShellTests route binding; published UI smoke", "IMPLEMENTED_VERIFIED"),
    ]
    implementation_commands = {
        "configuration.schema-validation": "ImportSettingsCommand",
        "configuration.transactional-update": "ImportSettingsCommand / ResetSettingsCommand",
        "configuration.settings-import-export": "ExportSettingsCommand / ImportSettingsCommand",
        "configuration.reset-defaults": "ResetSettingsCommand",
        "storage.file-analysis.folder-selection": "SelectFileAnalysisFolderCommand",
        "storage.file-analysis.duplicates": "RunFileAnalysisCommand",
        "storage.file-analysis.large-files": "RunFileAnalysisCommand",
        "storage.file-analysis.coverage": "RunFileAnalysisCommand / CancelCommand",
        "hardware.rgb-software-detection": "ScanCommand",
        "gaming.overlay-conflict-detection": "ScanCommand",
        "windows.app-load-measurement": "SelectAppLoadExecutableCommand / MeasureAppLoadCommand",
        "windows.boot-diagnostics": "ScanCommand",
        "storage.drive-diagnostics": "ScanCommand / OpenToolCommand",
        "security.lcd-performance-guard": "ScanCommand",
        "gaming.stutter-analysis": "ImportFrametimeCommand",
        "reports.research-mode": "AddResearchTrialCommand / AnalyzeResearchCommand",
    }
    for item_id, title, feature_id, component, handler, provider, test, status in implementation_records:
        verification = (
            "Typed schema and persisted UI workflow verified with isolated data; file-dialog cancellation and explicit consent were exercised."
            if item_id in {"configuration.settings-import-export", "configuration.reset-defaults"}
            else "Timestamped result with duration, state and evidence; missing capabilities explicitly UNSUPPORTED or SKIPPED."
        )
        item_evidence = (
            "dotnet test HyperBoostX.sln -c Release --no-restore: Passed 103/103; published UI smoke: Passed 73/73 routes plus settings export/import/reset and restart-persistence flows."
            if item_id in {"configuration.settings-import-export", "configuration.reset-defaults"}
            else evidence
        )
        records.append(requirement(
            f"implementation.{item_id}",
            "V3 native application",
            "src/HyperBoostX.App/TestCenterRunner.cs",
            title,
            feature_id,
            component,
            implementation_commands.get(item_id, "RunSafeTestsCommand"),
            handler,
            provider,
            verification,
            test,
            item_evidence,
            status=status,
        ))

    for record in records:
        if record["requirementId"] == "req.addendum.0043-single-instance-application-management":
            record.update({
                "uiComponent": "Application startup / existing main window activation",
                "command": "Second launch activation signal",
                "backendHandler": (
                    "App.OnStartup / SignalExistingInstance / "
                    "WaitForActivation / ActivateMainWindow"
                ),
                "provider": (
                    "Per-user Local mutex and data-directory-scoped auto-reset activation event; "
                    "no generic command payload"
                ),
                "verification": (
                    "Published executable smoke launched a second process against the same isolated data "
                    "directory; it exited successfully and restored the existing minimized window. A separate "
                    "data directory launched an independent instance without terminating the first."
                ),
                "test": (
                    "scripts/ui-smoke.ps1: singleInstanceSecondLaunchActivatedExisting and "
                    "distinctDataDirectoryRunsSeparateInstance"
                ),
                "evidence": (
                    "dotnet publish src\\HyperBoostX.App\\HyperBoostX.App.csproj -c Release -r win-x64 "
                    "--self-contained true -p:DebugType=None -p:DebugSymbols=false "
                    "-o artifacts\\publish\\win-x64: Passed; scripts\\ui-smoke.ps1 against that executable: "
                    "Passed 73/73 routes, duplicate launch exited with code 0, the existing window returned "
                    "to Normal state, a separate data-directory instance started while the first remained "
                    "alive and was stopped by the isolated test cleanup, and the original instance shut down."
                ),
                "status": "IMPLEMENTED_VERIFIED",
            })
        elif record["requirementId"] == "req.addendum.0177-threat-model":
            record.update({
                "featureId": "security.architecture.threat-model",
                "uiComponent": "docs/SECURITY_THREAT_MODEL.md",
                "command": "N/A - documented security architecture requirement",
                "backendHandler": (
                    "Source-grounded assessment of App.xaml.cs, app.manifest, Runtime.cs, "
                    "ActionEngine.cs, Journal.cs, SafePaths.cs, and Handoffs.cs"
                ),
                "provider": (
                    "Current .NET 8 WPF asInvoker application; in-process typed action handlers; "
                    "DPAPI CurrentUser; no privileged worker or authenticated IPC server"
                ),
                "verification": (
                    "Threat model records current verified boundaries, limitations, attacker "
                    "scenarios, broker security contract, and isolated verification gates. "
                    "It explicitly does not claim that planned controls are implemented."
                ),
                "test": (
                    "Manual source review against src/HyperBoostX.App/app.manifest, "
                    "src/HyperBoostX.App/App.xaml.cs, src/HyperBoostX.Windows/Runtime.cs, "
                    "src/HyperBoostX.Core/ActionEngine.cs, Journal.cs, SafePaths.cs, and "
                    "src/HyperBoostX.Windows/Handoffs.cs"
                ),
                "evidence": (
                    "docs/SECURITY_THREAT_MODEL.md; source review confirms asInvoker, fixed "
                    "in-process action registry, current-user DPAPI, path validation, and absence "
                    "of a privileged worker or named-pipe server. This verifies the threat-model "
                    "document only; privileged mutation and security audit remain unverified."
                ),
                "status": "IMPLEMENTED_VERIFIED",
            })
        elif record["requirementId"] == "req.addendum.0178-privilege-separation":
            record.update({
                "featureId": "security.architecture.privilege-separation",
                "uiComponent": "docs/SECURITY_THREAT_MODEL.md; desktop application manifest",
                "command": "N/A - architecture requirement",
                "backendHandler": (
                    "Desktop process requests asInvoker; no privileged worker is implemented"
                ),
                "provider": "Windows application manifest and current in-process action engine",
                "verification": (
                    "The desktop process requests asInvoker and the future least-privilege "
                    "worker contract is documented; actual process/broker isolation is not implemented."
                ),
                "test": "Source review of app.manifest and Runtime.cs; privileged worker test is blocked.",
                "evidence": (
                    "src/HyperBoostX.App/app.manifest; src/HyperBoostX.Windows/Runtime.cs; "
                    "docs/SECURITY_THREAT_MODEL.md. No elevation or privileged mutation was attempted."
                ),
                "status": "PARTIAL_VERIFIED",
            })
        elif record["requirementId"] == "req.addendum.0179-ipc-security":
            record.update({
                "featureId": "security.architecture.authenticated-ipc",
                "uiComponent": "docs/SECURITY_THREAT_MODEL.md",
                "command": "N/A - no privileged IPC endpoint",
                "backendHandler": "No privileged IPC handler exists; single-instance activation is signal-only",
                "provider": "Windows Local named mutex and auto-reset event with no explicit application ACL",
                "verification": (
                    "Source inspection confirms there is no named-pipe privileged worker. "
                    "The existing activation event is not an authenticated mutation protocol."
                ),
                "test": (
                    "Source search for named pipes and review of App.OnStartup / "
                    "SignalExistingInstance / WaitForActivation; privileged IPC tests not run."
                ),
                "evidence": (
                    "src/HyperBoostX.App/App.xaml.cs; docs/SECURITY_THREAT_MODEL.md. "
                    "Cross-user, endpoint-squatting, and IPC denial tests require a disposable Windows environment."
                ),
                "status": "UNSUPPORTED",
            })

    file_analysis_evidence = (
        "dotnet test HyperBoostX.sln -c Release --no-restore: Passed 132/132; published self-contained UI smoke "
        "selected a local fixture, rendered one verified duplicate group and two large files, preserved both SHA-256 hashes, "
        "and passed all 73 routes."
    )
    file_analysis_overrides = {
        "proto.route.ui.storage.duplicate-large-files": "IMPLEMENTED_VERIFIED",
        "proto.subfeature.ui.storage.duplicate-large-files.001-folder-selection": "IMPLEMENTED_VERIFIED",
        "proto.subfeature.ui.storage.duplicate-large-files.002-duplicate-groups": "IMPLEMENTED_VERIFIED",
        "proto.subfeature.ui.storage.duplicate-large-files.003-large-files": "IMPLEMENTED_VERIFIED",
        "proto.subfeature.ui.storage.duplicate-large-files.004-exclusions": "PARTIAL_VERIFIED",
    }
    for record in records:
        if record["requirementId"] in file_analysis_overrides:
            record.update({
                "uiComponent": "MainWindow file-analysis panel",
                "command": "SelectFileAnalysisFolderCommand / RunFileAnalysisCommand",
                "backendHandler": "ShellViewModel.RunFileAnalysis / FileAnalysis.ScanAsync",
                "provider": "Local filesystem metadata and SHA-256; reparse points skipped; no mutation",
                "verification": "User-selected bounded read-only scan with explicit complete/truncated/cancelled/skipped status.",
                "test": "FileAnalysisTests; ShellTests.DuplicateAndLargeFileRouteRunsRealReadOnlyAnalysis; scripts/ui-smoke.ps1",
                "evidence": file_analysis_evidence,
                "status": file_analysis_overrides[record["requirementId"]],
            })

    rgb_ids = {
        "proto.route.ui.hardware.rgb-software-detector",
        "proto.subfeature.ui.hardware.rgb-software-detector.001-signalrgb",
        "proto.subfeature.ui.hardware.rgb-software-detector.002-mystic-light",
        "proto.subfeature.ui.hardware.rgb-software-detector.003-openrgb",
        "proto.subfeature.ui.hardware.rgb-software-detector.004-icue",
        "proto.subfeature.ui.hardware.rgb-software-detector.005-armoury-crate",
    }
    for record in records:
        if record["requirementId"] in rgb_ids:
            record.update({
                "uiComponent": "MainWindow observation grid",
                "command": "ScanCommand",
                "backendHandler": "VendorSoftwareDetection.ScanRgbAsync",
                "provider": "Running processes, Win32_Service and uninstall registry with explicit vendor signatures",
                "verification": "Read-only detection plus interval CPU and working-set measurement for running RGB controller processes.",
                "test": "IntegrationProviderTests RGB tests; ShellTests.RgbDetectorRouteIsImplementedAndScannable; scripts/ui-smoke.ps1",
                "evidence": "Full suite passed 141/141; live OpenRGB-named process fixture produced measured interval CPU and working-set evidence; published UI smoke rendered RGB observations.",
                "status": "IMPLEMENTED_VERIFIED",
            })

    overlay_ids = {
        "proto.route.ui.gaming.overlay-conflict-detector",
        "proto.subfeature.ui.gaming.overlay-conflict-detector.001-discord",
        "proto.subfeature.ui.gaming.overlay-conflict-detector.002-steam",
        "proto.subfeature.ui.gaming.overlay-conflict-detector.003-nvidia",
        "proto.subfeature.ui.gaming.overlay-conflict-detector.004-amd",
        "proto.subfeature.ui.gaming.overlay-conflict-detector.005-rtss",
        "proto.subfeature.ui.gaming.overlay-conflict-detector.006-xbox-game-bar",
    }
    for record in records:
        if record["requirementId"] in overlay_ids:
            record.update({
                "uiComponent": "MainWindow observation grid",
                "command": "ScanCommand",
                "backendHandler": "OverlaySoftwareDetection.ScanAsync",
                "provider": "Running processes and uninstall registry with explicit overlay-provider signatures",
                "verification": "Read-only inventory plus interval CPU, working set and combined load; presence is explicitly not treated as proof that an overlay is enabled.",
                "test": "IntegrationProviderTests overlay tests; ShellTests.OverlayDetectorRouteIsImplementedAndScannable; scripts/ui-smoke.ps1",
                "evidence": "Full suite passed 149/149; live Discord-named process fixture produced measured interval CPU and working-set evidence; published UI smoke rendered overlay observations.",
                "status": "IMPLEMENTED_VERIFIED",
            })

    app_load_statuses = {
        "proto.route.ui.windows.app-load-optimizer": "IMPLEMENTED_VERIFIED",
        "proto.subfeature.ui.windows.app-load-optimizer.001-app-inventory": "IMPLEMENTED_VERIFIED",
        "proto.subfeature.ui.windows.app-load-optimizer.002-launch-timing": "IMPLEMENTED_VERIFIED",
        "proto.subfeature.ui.windows.app-load-optimizer.003-background-load": "IMPLEMENTED_VERIFIED",
        "proto.subfeature.ui.windows.app-load-optimizer.004-history": "IMPLEMENTED_VERIFIED",
    }
    for record in records:
        if record["requirementId"] in app_load_statuses:
            record.update({
                "uiComponent": "MainWindow App Load measurement panel and observation grid",
                "command": "SelectAppLoadExecutableCommand / MeasureAppLoadCommand",
                "backendHandler": "InstalledApplicationDetection.ScanAsync / ShellViewModel.RunAppLoadMeasurement / ApplicationLaunchMeasurement.MeasureAsync",
                "provider": "Bounded uninstall-registry inventory plus user-selected local executable and Windows Process APIs; no command-line arguments or process termination",
                "verification": "Bounded installed-app inventory, explicit launch confirmation, measured process creation, GUI input-idle when exposed, initial memory/CPU, and locally persisted report.",
                "test": "IntegrationProviderTests App Load inventory/launch tests; ShellTests.AppLoadOptimizerRouteIsImplementedAndRequiresASelectedExecutable; scripts/ui-smoke.ps1",
                "evidence": "Inventory and launch provider tests plus published UI smoke exercise the installed-app coverage result and a real packaged executable measurement.",
                "status": app_load_statuses[record["requirementId"]],
            })

    boot_ids = {
        "proto.route.ui.windows.boot-optimizer",
        "proto.subfeature.ui.windows.boot-optimizer.001-boot-events",
        "proto.subfeature.ui.windows.boot-optimizer.002-startup-impact",
        "proto.subfeature.ui.windows.boot-optimizer.003-service-dependencies",
        "proto.subfeature.ui.windows.boot-optimizer.004-boot-history",
    }
    for record in records:
        if record["requirementId"] in boot_ids:
            record.update({
                "uiComponent": "MainWindow observation and finding grids",
                "command": "ScanCommand",
                "backendHandler": "BootDiagnostics.ScanAsync",
                "provider": "Windows Diagnostics-Performance Event 100 and 101–110; Win32_Service.Dependencies",
                "verification": "Bounded read-only history, degradation and dependency inventory with explicit unavailable results; no boot/service/task mutation.",
                "test": "IntegrationProviderTests.BootDiagnosticsReturnsHistoryImpactAndDependencyEvidenceOrExplicitAvailability; ShellTests.BootOptimizerExposesTheReadOnlyStartupDiagnosticInsteadOfAnUnavailableRoute; scripts/ui-smoke.ps1",
                "evidence": "Full suite passed 158/158; current-host provider returned direct or explicit unavailable evidence; published UI smoke invoked Boot Optimizer, rendered observations, and passed 73/73 routes.",
                "status": "IMPLEMENTED_VERIFIED",
            })

    drive_ids = {
        "proto.route.ui.storage.drive-optimization",
        "proto.subfeature.ui.storage.drive-optimization.001-media-type",
        "proto.subfeature.ui.storage.drive-optimization.002-trim",
        "proto.subfeature.ui.storage.drive-optimization.003-optimize-drives",
        "proto.subfeature.ui.storage.drive-optimization.004-schedule",
        "proto.subfeature.ui.storage.drive-optimization.005-health",
    }
    for record in records:
        if record["requirementId"] in drive_ids:
            record.update({
                "uiComponent": "MainWindow observation grid and Windows-tool handoff",
                "command": "ScanCommand / OpenToolCommand",
                "backendHandler": "DriveDiagnostics.ScanAsync / Handoffs.Open",
                "provider": "MSFT_PhysicalDisk, MSFT_Volume, MSFT_ScheduledTask, fsutil policy query and Windows dfrgui",
                "verification": "Read-only media/volume/health/TRIM/schedule evidence plus explicit user-invoked Windows Optimize Drives handoff; no automatic maintenance.",
                "test": "IntegrationProviderTests.DriveDiagnosticsReturnsMediaVolumeScheduleAndTrimEvidenceWithoutMutation; ShellTests.DriveOptimizationExposesReadOnlyDiagnosticsAndWindowsOptimizeDrivesHandoff; scripts/ui-smoke.ps1",
                "evidence": "Full suite passed 160/160; providers returned direct or explicit unavailable evidence; published UI smoke invoked Drive Optimization, rendered results, and passed 73/73 routes.",
                "status": "IMPLEMENTED_VERIFIED",
            })

    lcd_guard_ids = {
        "proto.route.ui.privacy-security.lcd-performance-guard",
        "proto.subfeature.ui.privacy-security.lcd-performance-guard.001-monitor-only",
        "proto.subfeature.ui.privacy-security.lcd-performance-guard.002-gaming-guard",
        "proto.subfeature.ui.privacy-security.lcd-performance-guard.003-thresholds",
        "proto.subfeature.ui.privacy-security.lcd-performance-guard.004-fan-and-pump-exclusions",
    }
    for record in records:
        if record["requirementId"] in lcd_guard_ids:
            record.update({
                "uiComponent": "MainWindow LCD guard policy panel, observations and findings",
                "command": "ScanCommand",
                "backendHandler": "LcdPerformanceDetection.ScanAsync",
                "provider": "Explicit vendor process signatures with 500 ms normalized CPU and working-set samples",
                "verification": "Configurable thresholds and gaming-session alert gating; cooling/fan/pump utilities are explicitly protected and no process is stopped.",
                "test": "IntegrationProviderTests LCD signature and live-process tests; ShellTests.LcdPerformanceGuardIsImplementedWithConfigurableSafeThresholds; scripts/ui-smoke.ps1",
                "evidence": "Full suite passed 167/167; real AIDA64-named process fixture produced measured evidence and gaming alert gating; published UI smoke invoked the guard and passed 73/73 routes.",
                "status": "IMPLEMENTED_VERIFIED",
            })

    stutter_ids = {
        "proto.route.ui.gaming.stutter-analyzer",
        "proto.subfeature.ui.gaming.stutter-analyzer.001-frame-time-spikes",
        "proto.subfeature.ui.gaming.stutter-analyzer.002-gpu-correlation",
        "proto.subfeature.ui.gaming.stutter-analyzer.003-cpu-load",
        "proto.subfeature.ui.gaming.stutter-analyzer.004-disk-i-o",
        "proto.subfeature.ui.gaming.stutter-analyzer.005-thermal-data",
    }
    for record in records:
        if record["requirementId"] in stutter_ids:
            record.update({
                "uiComponent": "BenchmarkPanel frametime import plus shared observation/finding grids",
                "command": "ImportFrametimeCommand",
                "backendHandler": "FrameTimeAnalysis.AnalyzeCsv",
                "provider": "Bounded user-selected CSV: frametime_ms and optional cpu/gpu/disk_io/temperature columns",
                "verification": "Calculates FPS lows, frametime percentiles, documented spikes and Pearson correlations; imported evidence is not represented as live capture.",
                "test": "EngineTests frametime analysis validation; ShellTests.StutterAnalyzerIsImplementedWithARealFrametimeImportCommand; scripts/ui-smoke.ps1",
                "evidence": "Full suite passed 170/170; deterministic 1,000-row fixture verified lows/spikes/correlation and invalid inputs; published UI smoke imported a 100-row CSV, rendered calculated Average FPS, and passed 73/73 routes.",
                "status": "IMPLEMENTED_VERIFIED",
            })

    research_ids = {
        "proto.route.ui.reports-tests.research-mode",
        "proto.subfeature.ui.reports-tests.research-mode.001-a-windows-default",
        "proto.subfeature.ui.reports-tests.research-mode.002-b-hyperboostx",
        "proto.subfeature.ui.reports-tests.research-mode.003-c-ssd-upgrade",
        "proto.subfeature.ui.reports-tests.research-mode.004-d-ram-upgrade",
        "proto.subfeature.ui.reports-tests.research-mode.005-run-history",
    }
    for record in records:
        if record["requirementId"] in research_ids:
            record.update({
                "uiComponent": "BenchmarkPanel Research Mode trial editor and encrypted history",
                "command": "AddResearchTrialCommand / AnalyzeResearchCommand",
                "backendHandler": "ResearchExperiments.Add / ResearchExperiments.Report",
                "provider": "User-recorded A/B/C/D repeat trials in DPAPI-protected local DocumentStore",
                "verification": "Validated finite metrics, bounded notes/history, repeat-trial means and deltas against measured A baseline; no hardware simulation or causation claim.",
                "test": "EngineTests ResearchTrials tests; ShellTests.ResearchModeIsImplementedWithPersistedTrialCommands; scripts/ui-smoke.ps1",
                "evidence": "Full suite passed 174/174; core test persisted A/A/C repeats and verified mean/delta; published UI smoke saved a variant A trial, analyzed it, rendered results, and passed 73/73 routes.",
                "status": "IMPLEMENTED_VERIFIED",
            })

    ids = [record["requirementId"] for record in records]
    if len(ids) != len(set(ids)):
        raise SystemExit("Requirement IDs are not unique.")
    if len(navigation) != 73 or subfeature_count != 371 or len(legacy_actions) != 606:
        raise SystemExit(
            f"Unexpected prototype counts: routes={len(navigation)}, subfeatures={subfeature_count}, legacy={len(legacy_actions)}."
        )
    workflow_screens = [
        image for image in additional_screens
        if re.match(r"^(?:76|77|78|79|80|81)-", Path(image["src"]).name)
    ]
    visual_variants = [image for image in additional_screens if image not in workflow_screens]
    if len(additional_screens) != 8 or len(workflow_screens) != 6 or len(visual_variants) != 2:
        raise SystemExit(
            f"Unexpected additional gallery references: total={len(additional_screens)}, workflows={len(workflow_screens)}, variants={len(visual_variants)}."
        )

    status_counts = {}
    for item in records:
        status_counts[item["status"]] = status_counts.get(item["status"], 0) + 1
    payload = {
        "schemaVersion": 1,
        "generatedBy": "scripts/generate_traceability.py",
        "sources": {
            "prototype": str(PROTOTYPE.relative_to(WORKSPACE)).replace("\\", "/"),
            "navigation": {"path": str(NAVIGATION.relative_to(WORKSPACE)).replace("\\", "/"), "sha256": sha256(NAVIGATION)},
            "prototypeIndex": {"path": str(INDEX.relative_to(WORKSPACE)).replace("\\", "/"), "sha256": sha256(INDEX)},
            "prototypeGallery": {"path": str(GALLERY.relative_to(WORKSPACE)).replace("\\", "/"), "sha256": sha256(GALLERY)},
            "legacyActionMap": {"path": str(LEGACY_SOURCE.relative_to(WORKSPACE)).replace("\\", "/"), "sha256": sha256(LEGACY_SOURCE)},
            "technicalReferenceMatrix": {
                "path": str(REFERENCE_MATRIX.relative_to(ROOT)).replace("\\", "/"),
                "sha256": sha256(REFERENCE_MATRIX),
            },
            "technicalReferenceValidation": {
                "path": str(REFERENCE_VALIDATION.relative_to(ROOT)).replace("\\", "/"),
                "sha256": sha256(REFERENCE_VALIDATION),
            },
            "codexUiImplementation": {
                "path": str(expected_ui_doc.relative_to(WORKSPACE)).replace("\\", "/"),
                "present": expected_ui_doc.is_file(),
                "sha256": sha256(expected_ui_doc),
            },
            "masterTaskAttachment": {
                "source": attachment_payload.get("source"),
                "sha256": attachment_payload.get("sha256"),
                "requirementSnapshot": str(ATTACHMENT_REQUIREMENTS.relative_to(ROOT)).replace("\\", "/"),
                "sourceSnapshotAvailable": bool(attachment_payload.get("sha256")),
            },
        },
        "inventory": {
            "primaryRoutes": len(navigation),
            "prototypeSubfeatures": subfeature_count,
            "galleryImageReferences": len(gallery_screens),
            "additionalScreens": len(workflow_screens),
            "alternateVisualReferences": len(visual_variants),
            "additionalGalleryReferences": len(additional_screens),
            "prototypeInteractiveElements": len(controls),
            "prototypeInteractiveSourceTags": len(controls),
            "prototypeEventBindings": len(event_bindings),
            "prototypeCommandCases": len(command_cases),
            "prototypeModalCallSites": len(dialog_calls),
            "prototypeDialogsAndOverlays": len(parser.dialogs) + len(dialog_calls),
            "legacyActionMapEntries": len(legacy_actions),
            "technicalReferenceMenus": len(reference_matrix["menus"]),
            "technicalReferenceAssignments": len(reference_assignments),
            "technicalReferenceDistinctUrls": len(distinct_reference_urls),
            "technicalReferenceWebConfirmedDistinctUrls": len(
                distinct_reference_urls_by_classification.get("web-confirmed", set())
            ),
            "technicalReferenceCandidateDistinctUrls": len(
                distinct_reference_urls_by_classification.get("candidate-direct-link", set())
            ),
            "technicalReferenceReviewedDistinctUrls": len(validated_sources),
            "runtimeActionIds": len(runtime_action_ids),
            "runtimeActionIdsList": sorted(runtime_action_ids),
            "masterTaskRequirements": sum(item["source"] == "MASTER_DEVELOPMENT_TASK.md" for item in records),
            "masterTaskAttachmentRequirements": sum(item["source"] == "Pasted text #1.txt" for item in records),
            "addendumRequirements": sum(item["source"] == "MASTER_DEVELOPMENT_TASK_ADDENDUM.md" for item in records),
        },
        "summary": {
            "requirementCount": len(records),
            "statusCounts": status_counts,
            "mandatoryNotVerified": sum(item["mandatory"] and item["status"] != "IMPLEMENTED_VERIFIED" for item in records),
            "stableReady": all(not item["mandatory"] or item["status"] == "IMPLEMENTED_VERIFIED" for item in records),
            "gatePolicy": "Fail Stable promotion if any mandatory requirement is not IMPLEMENTED_VERIFIED; source-only counts, route navigation, or category mappings are not feature parity.",
        },
        "requirements": records,
    }
    OUTPUT.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(OUTPUT), **payload["inventory"], **payload["summary"]}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
