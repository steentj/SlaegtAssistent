import json
import re
from pathlib import Path

MAX_INPUT = 20 * 1024 * 1024
MAX_REQUEST = 64 * 1024


class ProtocolError(Exception):
    def __init__(self, code):
        self.code = code
        super().__init__(code)


def read_request(request_path):
    path = Path(request_path)
    if not path.is_absolute() or path.is_symlink() or not path.is_file() or path.stat().st_size > MAX_REQUEST:
        raise ProtocolError("invalid-input")
    root = path.parent.absolute()
    try:
        data = json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=_unique_object)
    except (ValueError, UnicodeError, OSError):
        raise ProtocolError("invalid-input") from None
    if not isinstance(data, dict) or set(data) != {"protocolVersion", "jobId", "operation", "inputPath", "outputPath", "assetRoot", "options"}:
        raise ProtocolError("invalid-input")
    if type(data["protocolVersion"]) is not int or data["protocolVersion"] != 1:
        raise ProtocolError("unsupported-version")
    if not isinstance(data["jobId"], str) or not re.fullmatch(r"[A-Za-z0-9_-]{1,80}", data["jobId"]):
        raise ProtocolError("invalid-input")
    if data["operation"] != "graph-pdf":
        raise ProtocolError("invalid-input")
    options = data["options"]
    if not isinstance(options, dict) or set(options) != {"paper", "orientation"} or options["paper"] not in ("A4", "A3") or options["orientation"] not in ("portrait", "landscape"):
        raise ProtocolError("invalid-input")
    input_path = _job_path(root, data["inputPath"], ".html", True)
    output_path = _job_path(root, data["outputPath"], ".pdf", False)
    asset_root = _job_path(root, data["assetRoot"], None, True)
    if not input_path.is_file() or input_path.stat().st_size > MAX_INPUT or not asset_root.is_dir() or output_path.exists():
        raise ProtocolError("invalid-input")
    return data, input_path, output_path


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("duplicate")
        result[key] = value
    return result


def _job_path(root, value, suffix, must_exist):
    if not isinstance(value, str):
        raise ProtocolError("invalid-input")
    path = Path(value)
    if not path.is_absolute() or (suffix and path.suffix.lower() != suffix):
        raise ProtocolError("invalid-input")
    try:
        resolved = path.resolve(strict=must_exist)
        resolved_root = root.resolve(strict=True)
        if not resolved.is_relative_to(resolved_root) or resolved == resolved_root:
            raise ProtocolError("invalid-input")
        # Symbolske links afvises også når de peger tilbage ind i jobmappen.
        relative = path.relative_to(root)
        current = root
        for part in relative.parts:
            current = current / part
            if current.is_symlink():
                raise ProtocolError("invalid-input")
    except (OSError, ValueError, RuntimeError):
        raise ProtocolError("invalid-input") from None
    if not must_exist and not resolved.parent.is_dir():
        raise ProtocolError("invalid-input")
    return path
