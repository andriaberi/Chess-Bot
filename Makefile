CONFIG ?= Release

export CONFIG

.DEFAULT_GOAL := help
.PHONY: help install build run publish clean test check format version bump

# `make publish` opens a picker; `make publish RID=linux-x64` skips it.
publish:
	@bash cli.sh publish $(RID)

# `make bump TO=patch|minor|major|1.2.3` (TO defaults to patch).
bump:
	@bash cli.sh bump $(TO)

help install build run clean test check format version:
	@bash cli.sh $@
