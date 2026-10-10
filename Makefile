CONFIG ?= Release

export CONFIG

.DEFAULT_GOAL := help
.PHONY: help install build run publish clean test perft check format match

# `make publish` opens a picker; `make publish RID=linux-x64` skips it.
publish:
	@bash cli.sh publish $(RID)

# `make match OLD=v1.0` plays the working tree against v1.0; NEW=<version>, GAMES and TC are optional.
match:
	@GAMES=$(GAMES) TC=$(TC) bash cli.sh match $(OLD) $(NEW)

help install build run clean test perft check format:
	@bash cli.sh $@
