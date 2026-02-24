# Stafr

Stafr generates laser-ready SVG labels ("staves") for historical sewing pattern storage.

## Philosophy

- Open source (GPLv3)
- Deterministic output
- Simple templates
- Small, readable steps
- No magic

## Usage

```bash
dotnet run --project src/Stafr.Cli -- \
    --input examples/tunika.yaml \
    --template templates/stick-template.svg \
    --output output/tunika.svg