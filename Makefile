CONFIG ?= Release

export CONFIG

.DEFAULT_GOAL := help
.PHONY: help install build run publish clean test perft check format

# `make publish` opens a picker; `make publish RID=linux-x64` skips it.
publish:
	@bash cli.sh publish $(RID)

help install build run clean test perft check format:
	@bash cli.sh $@
