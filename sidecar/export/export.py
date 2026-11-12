import argparse
import contextlib
import json
import os
import sys

from protocol import ProtocolError, read_request
from pdf_renderer import render_pdf


def main():
    parser = argparse.ArgumentParser()
    action = parser.add_mutually_exclusive_group(required=True)
    action.add_argument("--request")
    action.add_argument("--version", action="store_true")
    args = parser.parse_args()
    if args.version:
        try:
            with open(os.devnull, "w", encoding="utf-8") as sink, contextlib.redirect_stdout(sink), contextlib.redirect_stderr(sink):
                import weasyprint
            print(json.dumps({"protocolVersion": 1, "toolVersion": "0.1.0", "rendererVersion": weasyprint.__version__}, separators=(",", ":")))
            return 0
        except (ImportError, OSError):
            print(json.dumps({"protocolVersion": 1, "errorCode": "missing-tool"}, separators=(",", ":")))
            return 1
    job_id = ""
    try:
        data, input_path, output_path = read_request(args.request)
        job_id = data["jobId"]
        # Tredjepartsbiblioteker må ikke blande deres output med protokollens JSON.
        with open(os.devnull, "w", encoding="utf-8") as sink, contextlib.redirect_stdout(sink), contextlib.redirect_stderr(sink):
            render_pdf(input_path, output_path, data["options"])
        result = {"protocolVersion": 1, "jobId": job_id, "status": "ok", "outputPath": str(output_path), "warnings": []}
        code = 0
    except ProtocolError as error:
        result = {"protocolVersion": 1, "jobId": job_id, "status": "error", "errorCode": error.code, "warnings": []}
        code = 1
    except Exception:
        result = {"protocolVersion": 1, "jobId": job_id, "status": "error", "errorCode": "conversion-failed", "warnings": []}
        code = 1
    print(json.dumps(result, ensure_ascii=False, separators=(",", ":")))
    return code


if __name__ == "__main__":
    sys.exit(main())
