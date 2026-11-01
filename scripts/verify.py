"""Build, run real geometry tests and CLI smoke checks. No third-party Python packages."""
from pathlib import Path
import hashlib
import json
import os
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
DOTNET = os.environ.get("PANATTA_DOTNET", "dotnet")
os.environ.setdefault("DOTNET_CLI_HOME", str(ROOT / ".tools" / "dotnet-home"))
os.environ.setdefault("NUGET_PACKAGES", str(ROOT / ".tools" / "nuget"))
os.environ.setdefault("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
os.environ.setdefault("DOTNET_NOLOGO", "1")


def run(*args, expected=0):
    p = subprocess.run(list(args), cwd=ROOT, capture_output=True, text=True)
    if p.returncode != expected:
        raise RuntimeError(f"{args}: exit {p.returncode}, expected {expected}\n{p.stdout}\n{p.stderr}")
    return p


print(run(DOTNET, "run", "--project", "tests/Panatta.Tests", "-c", "Release").stdout)
print(run(DOTNET, "build", "src/Panatta.Cli", "-c", "Release").stdout)
dll = ROOT / "src/Panatta.Cli/bin/Release/net8.0/Panatta.Cli.dll"
cli_checks = 0
with tempfile.TemporaryDirectory(prefix="panatta-") as tmp:
    tmp = Path(tmp)

    def cli(source, name, expected=0, *extra):
        global cli_checks
        cli_checks += 1
        return run(DOTNET, str(dll), "--input", str(source), "--output", str(tmp / name), *extra, expected=expected)

    src = ROOT / "examples/rectangle.json"
    cli(src, "retângulo")
    result = json.loads((tmp / "retângulo.result.json").read_text())
    assert result["result"]["areaM2"] == 200
    assert result["result"]["perimeterM"] == 60
    assert result["inputSha256"] == hashlib.sha256(src.read_bytes()).hexdigest()
    assert len((tmp / "retângulo.segments.csv").read_text().splitlines()) == 5
    assert "RASCUNHO" in (tmp / "retângulo.report.txt").read_text()
    before = (tmp / "retângulo.result.json").read_bytes()
    cli(src, "retângulo", 3)
    assert before == (tmp / "retângulo.result.json").read_bytes()
    cli(ROOT / "examples/semicircle.json", "arco")
    assert "azimute da corda" in (tmp / "arco.report.txt").read_text()
    modified = json.loads(src.read_text())
    modified["closed"] = False
    bad = tmp / "open.json"
    bad.write_text(json.dumps(modified))
    cli(bad, "aberta", 2)
    invalid = json.loads((tmp / "aberta.result.json").read_text())["result"]
    assert invalid["status"] == "invalid" and invalid["segments"] == []
    assert any(x["code"] == "OPEN" for x in invalid["issues"])
    bad.write_text('{"schemaVersion":"1.0", "extra": true}')
    cli(bad, "unknown", 3)
    assert not (tmp / "unknown.result.json").exists()
    bad.write_text('{"vertices": null}')
    cli(bad, "null", 3)
    bad.write_text('{ not json }')
    cli(bad, "malformed", 3)
    cli(src, "decimal", 0, "--coordinate-decimals", "2", "--distance-decimals", "1")
    assert "E=0,00 N=0,00" in (tmp / "decimal.report.txt").read_text()
    cli(src, "bad-decimal", 3, "--coordinate-decimals", "12")
    bad.write_text(src.read_text().replace('"e": 10', '"e": 1e309'))
    cli(bad, "nonfinite", 3)
    assert not (tmp / "nonfinite.result.json").exists()
    modified = json.loads(src.read_text())
    modified["vertices"][0]["e"] = None
    bad.write_text(json.dumps(modified))
    cli(bad, "null-coordinate", 3)
    modified = json.loads(src.read_text())
    del modified["vertices"][0]["e"]
    bad.write_text(json.dumps(modified))
    cli(bad, "missing-coordinate", 3)
    # A late collision must not remove an earlier user-owned output.
    (tmp / "collision.segments.csv").write_text("preserve")
    cli(src, "collision", 3)
    assert (tmp / "collision.segments.csv").read_text() == "preserve"
    assert not (tmp / "collision.result.json").exists()

print(f"CLI: {cli_checks} smoke checks passed (exit status, JSON/CSV/text, hash, precision, invalid inputs, no overwrite).")

# Static check only: no Autodesk/AutoLISP runtime is available here.
text = (ROOT / "cad/PANATTAMEMORIAL.lsp").read_text()
depth = 0
string = escaped = comment = False
for c in text:
    if comment:
        if c == "\n": comment = False
    elif string:
        if escaped: escaped = False
        elif c == "\\": escaped = True
        elif c == '"': string = False
    elif c == ";": comment = True
    elif c == '"': string = True
    elif c == "(": depth += 1
    elif c == ")":
        depth -= 1
        assert depth >= 0, "LISP: unexpected closing parenthesis"
assert depth == 0 and not string, "LISP: unterminated expression/string"
assert "(entmod" not in text and "(setvar" not in text
print("LISP: structural static check passed; execution in Civil 3D NOT RUN.")
