#!/usr/bin/env bash
# Chess Bot developer CLI, driven by the Makefile. Run `./cli.sh help` for usage.

set -uo pipefail
cd "$(dirname "$0")"

SOLUTION=Chess-Bot.sln
PROJECT=Chess-Bot.csproj
CONFIG=${CONFIG:-Release}
DIST_DIR=${DIST_DIR:-dist}
MATCH_DIR=${MATCH_DIR:-.match}
RUNTIMES=(linux-x64 win-x64 osx-x64 osx-arm64)

# Style

# Colour on a terminal or in GitHub Actions; NO_COLOR turns it off.
if [[ -z ${NO_COLOR:-} && ( -t 1 || -n ${GITHUB_ACTIONS:-} ) ]]; then
	BOLD=$'\e[1m' DIM=$'\e[2m' RESET=$'\e[0m'
	ACCENT=$'\e[36m' GREEN=$'\e[32m' RED=$'\e[31m' YELLOW=$'\e[33m'
else
	BOLD='' DIM='' RESET='' ACCENT='' GREEN='' RED='' YELLOW=''
fi

ok()   { printf '%s✓%s %s\n' "$GREEN" "$RESET" "$*"; }
note() { printf '%s•%s %s\n' "$YELLOW" "$RESET" "$*"; }
fail() { printf '%s✗%s %s\n' "$RED" "$RESET" "$*" >&2; }

# rule — a dim line across the terminal.
rule() {
	local line
	printf -v line '%*s' "$(tput cols 2>/dev/null || echo 40)" ''
	printf '%s%s%s\n' "$DIM" "${line// /─}" "$RESET"
}

# Picker

UI_ACTIVE=0

ui_enter() {
	exec 3<>/dev/tty
	UI_STTY=$(stty -g <&3)
	stty -echo -icanon <&3
	printf '\e[?1049h\e[?25l' >&3   # alternate screen, hide cursor
	UI_ACTIVE=1
}

ui_leave() {
	(( UI_ACTIVE )) || return 0
	printf '\e[?25h\e[?1049l' >&3   # show cursor, restore screen
	stty "$UI_STTY" <&3
	exec 3>&-
	UI_ACTIVE=0
}

STEP_LOG=''
cleanup() { ui_leave; [[ -n $STEP_LOG ]] && rm -f "$STEP_LOG"; return 0; }

trap cleanup EXIT
trap 'exit 130' INT TERM

# pick TITLE OPTION... — sets PICKED; returns 1 if the user cancels.
pick() {
	local title=$1; shift
	local options=("$@") count=$# sel=0 i key rest frame

	ui_enter
	while :; do
		frame=$'\e[H\e[J'"${BOLD}${title}${RESET}"$'\n\n'
		for i in "${!options[@]}"; do
			if (( i == sel )); then
				frame+="${ACCENT}${BOLD}❯ ${options[i]}${RESET}"$'\n'
			else
				frame+="  ${BOLD}${options[i]}${RESET}"$'\n'
			fi
		done
		frame+=$'\n'"${DIM}↑↓ select · enter confirm · esc cancel${RESET}"
		printf '%s' "$frame" >&3

		IFS= read -rsn1 -u 3 key
		case $key in
			$'\e')
				IFS= read -rsn2 -t 0.05 -u 3 rest
				case ${rest:1} in
					A) (( sel = (sel - 1 + count) % count )) ;;
					B) (( sel = (sel + 1) % count )) ;;
					'') ui_leave; return 1 ;;
				esac ;;
			k) (( sel = (sel - 1 + count) % count )) ;;
			j) (( sel = (sel + 1) % count )) ;;
			q) ui_leave; return 1 ;;
			'') break ;;
		esac
	done
	ui_leave

	PICKED=${options[sel]}
}

# Steps

# step LABEL DONE CMD... — runs CMD quietly behind a spinner, printing its log only on failure.
# The log stays in $STEP_LOG until the next step, so callers can read results from it.
step() {
	local label=$1 done=$2; shift 2
	local pid status

	[[ -n $STEP_LOG ]] && rm -f "$STEP_LOG"
	STEP_LOG=$(mktemp)

	"$@" >"$STEP_LOG" 2>&1 &
	pid=$!
	trap 'kill $pid 2>/dev/null; printf "\r\e[K\e[?25h"; exit 130' INT TERM

	if [[ -t 1 ]]; then
		local frames=(⠋ ⠙ ⠹ ⠸ ⠼ ⠴ ⠦ ⠧ ⠇ ⠏) i=0
		printf '\e[?25l'
		while kill -0 "$pid" 2>/dev/null; do
			printf '\r%s%s%s %s' "$ACCENT" "${frames[i++ % 10]}" "$RESET" "$label"
			sleep 0.08
		done
		printf '\r\e[K\e[?25h'
	fi

	wait "$pid"; status=$?
	trap 'exit 130' INT TERM

	if (( status == 0 )); then
		ok "$done"
	else
		fail "$label failed"
		cat "$STEP_LOG" >&2
	fi
	return "$status"
}

require_dotnet() {
	if ! command -v dotnet >/dev/null; then
		fail "dotnet not found; install the .NET SDK from https://dotnet.microsoft.com/download"; exit 1
	fi
}

# Commands

cmd_help() {
	local A=$ACCENT R=$RESET B=$BOLD D=$DIM
	cat <<EOF
${B}Chess Bot${R} ${D}— make <command>${R}

${B}Build${R}
  ${A}install${R}   Restore NuGet packages
  ${A}build${R}     Build the app and tests             
  ${A}run${R}       Build and play the game
  ${A}publish${R}   Package the game for a platform    
  ${A}clean${R}     Remove build artifacts

${B}Test${R}
  ${A}test${R}      Run the tests
  ${A}perft${R}     Run the perft tests as a table
  ${A}check${R}     Build everything and test           
  ${A}format${R}    Fix whitespace to match .editorconfig

${B}Engine${R}
  ${A}match${R}     Play two versions against each other

${D}Without RID, publish opens a picker.${R}
${D}match OLD=v1.0 plays your working tree against v1.0; NEW=<version> picks another side,
GAMES=200 and TC=10+0.1 set the length. Needs fastchess or cutechess-cli on PATH.${R}
EOF
}

cmd_install() {
	require_dotnet
	step "Restoring packages" "Restored packages" dotnet restore "$SOLUTION"
}

cmd_build() {
	require_dotnet
	step "Building chess-bot" "Built chess-bot $DIM→ bin/$CONFIG/$RESET" \
		dotnet build "$SOLUTION" -c "$CONFIG" -nologo
}

cmd_run() {
	require_dotnet
	step "Building chess-bot" "Built chess-bot $DIM→ bin/$CONFIG/$RESET" \
		dotnet build "$PROJECT" -c "$CONFIG" -nologo || exit 1
	exec dotnet run --project "$PROJECT" -c "$CONFIG" --no-build
}

cmd_test() {
	require_dotnet
	step "Running tests" "Tests passed" dotnet test "$SOLUTION" -c "$CONFIG" -nologo || return 1

	# "Passed!  - Failed: 0, Passed: 55, Skipped: 3, Total: 58, Duration: 18 s - ..." -> "55 passed, 3 skipped in 18 s"
	local summary
	summary=$(sed -nE 's/.*Passed: +([0-9]+), Skipped: +([0-9]+), Total: +[0-9]+, Duration: +([^-]*[^ -]) +-.*/\1 passed, \2 skipped in \3/p' "$STEP_LOG" | head -1)
	[[ -n $summary ]] && printf '  %s%s%s\n' "$DIM" "$summary" "$RESET"
	return 0
}

# perft — runs the perft tests and prints their results as a table.
cmd_perft() {
	require_dotnet
	local results status=0
	results=$(mktemp -d)

	step "Running perft" "Ran perft" \
		dotnet test Tests/Tests.csproj -c "$CONFIG" -nologo --filter "FullyQualifiedName~PerftTests" \
		--logger "trx;LogFileName=perft.trx" --results-directory "$results" || status=1

	[[ -f $results/perft.trx ]] && perft_table "$results/perft.trx"
	rm -rf "$results"
	return "$status"
}

# perft_table TRX — one row per position and depth, read from the test results file.
perft_table() {
	# Each result looks like testName="...Perft(name: &quot;Kiwipete&quot;, fen: ..., depth: 2, expected: 2039)" duration="00:00:00.0012" outcome="Passed"
	grep -o '<UnitTestResult [^>]*>' "$1" \
		| sed -E 's/.*name: &quot;([^&]*)&quot;.*depth: ([0-9]+), expected: ([0-9]+)\)".*duration="([0-9:.]+)".*outcome="([A-Za-z]+)".*/\1|\2|\3|\4|\5/' \
		| sort -t'|' -k1,1 -k2,2n \
		| awk -F'|' -v B="$BOLD" -v D="$DIM" -v R="$RESET" -v G="$GREEN" -v X="$RED" '
			function commas(n,   s, out) {
				s = sprintf("%d", n); out = ""
				while (length(s) > 3) { out = "," substr(s, length(s) - 2) out; s = substr(s, 1, length(s) - 3) }
				return s out
			}
			function duration(ms) {
				if (ms < 1) return "<1 ms"
				if (ms < 1000) return sprintf("%d ms", ms)
				return sprintf("%.2f s", ms / 1000)
			}
			# Blank when the run is too short to time meaningfully
			function speed(nodes, ms) {
				if (ms < 1) return ""
				nodes = nodes / ms * 1000
				return nodes >= 1e6 ? sprintf("%.1f M/s", nodes / 1e6) : sprintf("%.0f k/s", nodes / 1e3)
			}
			function repeat(text, count,   out) { out = ""; while (count-- > 0) out = out text; return out }
			# border LEFT MIDDLE RIGHT — a horizontal line across all columns
			function border(left, middle, right,   i, line) {
				line = left
				for (i = 1; i <= columns; i++) line = line repeat("─", width[i] + 2) (i < columns ? middle : right)
				return D line R
			}
			BEGIN {
				columns = split("10 5 12 7 8 1", width, " ")
				bar = D "│" R
				print border("┌", "┬", "┐")
				printf "%s %s%-10s%s %s %s%5s%s %s %s%12s%s %s %s%7s%s %s %s%8s%s %s   %s\n", \
					bar, B, "Position", R, bar, B, "Depth", R, bar, B, "Nodes", R, \
					bar, B, "Time", R, bar, B, "Speed", R, bar, bar
				print border("├", "┼", "┤")
			}
			{
				split($4, t, ":"); ms = (t[1] * 3600 + t[2] * 60 + t[3]) * 1000; total += ms
				if ($1 != previous && NR > 1) print border("├", "┼", "┤")
				name = $1 == previous ? "" : $1; previous = $1
				if ($5 == "Passed") { mark = G "✓" R; passed++ } else { mark = X "✗" R; failed++ }
				printf "%s %-10s %s %5d %s %12s %s %7s %s %8s %s %s %s\n", \
					bar, name, bar, $2, bar, commas($3), bar, duration(ms), bar, speed($3, ms), bar, mark, bar
			}
			END {
				print border("└", "┴", "┘")
				printf "  %d passed", passed
				if (failed) printf ", %s%d failed%s", X, failed, R
				printf " in %s\n", duration(total)
			}'
}

cmd_check() {
	local status=0
	cmd_build || status=1
	if (( status == 0 )); then
		cmd_test || status=1
	fi
	printf '\n'
	if (( status == 0 )); then
		printf '%s%sAll checks passed.%s\n' "$GREEN" "$BOLD" "$RESET"
	else
		printf '%s%sChecks failed.%s\n' "$RED" "$BOLD" "$RESET"
	fi
	return "$status"
}

cmd_format() {
	require_dotnet
	step "Formatting" "Formatted code to match .editorconfig" \
		bash -c 'dotnet format whitespace "$1" && dotnet format whitespace Tests/Tests.csproj' _ "$PROJECT"
}

cmd_publish() {
	require_dotnet
	local rid=${1:-}
	if [[ -z $rid ]]; then
		if [[ -t 0 && -t 1 ]]; then
			pick Publish "${RUNTIMES[@]}" || exit 0
			rid=$PICKED
		else
			fail "No terminal for the picker; pass RID=<$(IFS='|'; echo "${RUNTIMES[*]}")>"; exit 1
		fi
	fi

	local out=$DIST_DIR/chess-bot-$rid
	rm -rf "$out"
	step "Publishing $rid" "Published $rid $DIM→ $out/$RESET" \
		dotnet publish "$PROJECT" -c Release -r "$rid" --self-contained -nologo -o "$out" || exit 1

	# The app loads fonts, sprites, sounds and openings from Resources next to where it runs
	cp -r Resources "$out/"
	note "Run it from inside that folder so it can find Resources"
}

cmd_clean() {
	rm -rf bin obj Tests/bin Tests/obj TestResults "$DIST_DIR" "$MATCH_DIR"
	ok "Removed bin/, obj/, Tests/bin/, Tests/obj/, $DIST_DIR/ and $MATCH_DIR/"
}

# match OLD [NEW] — builds two versions of the bot and plays them against each other.
# OLD and NEW are tags, commits or branches; NEW defaults to the working tree, uncommitted changes included.
# Both sides start from the same random openings with each color (-repeat), so the result measures the engines, not the openings.
cmd_match() {
	require_dotnet
	local old=${1:-} new=${2:-} runner
	local games=${GAMES:-200} tc=${TC:-10+0.1}
	local concurrency=$(( $(nproc 2>/dev/null || echo 2) / 2 ))
	(( concurrency < 1 )) && concurrency=1

	if [[ -z $old ]]; then
		fail "Name the version to play against, e.g. make match OLD=v1.0"; exit 1
	fi
	if command -v fastchess >/dev/null; then runner=fastchess
	elif command -v cutechess-cli >/dev/null; then runner=cutechess-cli
	else
		fail "Needs fastchess (https://github.com/Disservin/fastchess) or cutechess-cli (https://github.com/cutechess/cutechess) on PATH"
		exit 1
	fi

	match_build "$old" old || exit 1
	if [[ -n $new ]]; then
		match_build "$new" new || exit 1
	else
		step "Building the working tree" "Built the working tree $DIM→ $MATCH_DIR/new/$RESET" \
			dotnet build "$PROJECT" -c Release -nologo -o "$MATCH_DIR/new" || exit 1
	fi

	# Names come from the engines themselves (e.g. "Chess-Bot 1.2+3"), marked old and new in case they match
	local old_name new_name
	old_name="$(match_engine_name "$MATCH_DIR/old") (old)"
	new_name="$(match_engine_name "$MATCH_DIR/new") (new)"
	local pgn=$MATCH_DIR/games.pgn
	rm -f "$pgn"

	note "$new_name vs $old_name: $games games at $tc, $concurrency at a time, with $runner"
	rule
	local args=(
		-engine "cmd=$MATCH_DIR/new/Chess-Bot" "name=$new_name"
		-engine "cmd=$MATCH_DIR/old/Chess-Bot" "name=$old_name"
	)
	if [[ $runner == fastchess ]]; then
		args+=(-each proto=uci args=--uci "tc=$tc" -pgnout "file=$pgn")
	else
		args+=(-each proto=uci arg=--uci "tc=$tc" -pgnout "$pgn")
	fi
	args+=(
		-openings file=Resources/Openings/Match.epd format=epd order=random
		-repeat -games 2 -rounds $(( (games + 1) / 2 )) -concurrency "$concurrency"
	)
	local log=$MATCH_DIR/match.log
	"$runner" "${args[@]}" | tee "$log"
	rule
	ok "Games saved $DIM→ $pgn$RESET"
	match_verdict "$log" "$games"
}

# match_verdict LOG GAMES — the final result in plain words: whether the new version is stronger, weaker,
# or not clearly different yet, and by how much. The range is the runner's 95% confidence interval.
match_verdict() {
	local log=$1 games=$2 elo margin verdict

	# The runner reprints results as it goes, so the last line counts
	# fastchess: "Elo: 70.44 +/- 154.21, nElo: ..."    cutechess-cli: "Elo difference: 70.4 +/- 154.2, LOS: ..."
	read -r elo margin < <(sed -nE 's/^Elo( difference)?: *([-+0-9.a-z]+) *\+\/- *([-+0-9.a-z]+).*/\2 \3/p' "$log" | tail -n 1)
	if [[ -z $elo ]]; then
		note "No result to judge; the match may have been stopped before any game finished"
		return 0
	fi

	verdict=$(awk -v elo="$elo" -v margin="$margin" -v games="$games" 'BEGIN {
		if (elo ~ /^-inf/) { print "weaker|New version lost every game: far weaker, too big a gap to put a number on"; exit }
		if (elo ~ /^inf/)  { print "stronger|New version won every game: far stronger, too big a gap to put a number on"; exit }
		low = elo - margin; high = elo + margin
		if (low > 0)
			printf "stronger|New version is stronger by about %d Elo (somewhere between %d and %d)\n", elo, low, high
		else if (high < 0)
			printf "weaker|New version is weaker by about %d Elo (somewhere between %d and %d)\n", -elo, -high, -low
		else
			printf "unclear|No clear difference yet (somewhere between %+d and %+d Elo). Play more games: GAMES=%d\n", low, high, games * 4
	}')

	case ${verdict%%|*} in
		stronger) ok "${BOLD}${verdict#*|}${RESET}" ;;
		weaker)   fail "${BOLD}${verdict#*|}${RESET}" ;;
		*)        note "${BOLD}${verdict#*|}${RESET}" ;;
	esac
}

# match_build REF NAME — checks REF out in a temporary worktree and builds it into $MATCH_DIR/NAME.
match_build() {
	local ref=$1 out=$MATCH_DIR/$2 src=$MATCH_DIR/src-$2

	if ! git rev-parse --verify --quiet "$ref^{commit}" >/dev/null; then
		fail "No tag, commit or branch named '$ref'"; return 1
	fi
	# Older versions would open the game window instead of answering UCI commands
	if ! git cat-file -e "$ref:Scripts/Framework/App/Core/Uci.cs" 2>/dev/null; then
		fail "$ref has no UCI mode, so it can't play matches; versions from v1.0 on can"; return 1
	fi

	git worktree remove --force "$src" >/dev/null 2>&1
	rm -rf "$src" "$out"
	step "Checking out $ref" "Checked out $ref" git worktree add --detach --force "$src" "$ref" || return 1
	step "Building $ref" "Built $ref $DIM→ $out/$RESET" dotnet build "$src/$PROJECT" -c Release -nologo -o "$out"
	local status=$?
	git worktree remove --force "$src" >/dev/null 2>&1
	return "$status"
}

# match_engine_name DIR — the name the engine in DIR gives over UCI, e.g. "Chess-Bot 1.2".
match_engine_name() {
	printf 'uci\nquit\n' | "$1/Chess-Bot" --uci | sed -n 's/^id name //p'
}

case ${1:-help} in
	help|install|build|run|publish|clean|test|perft|check|format|match) cmd=$1; shift; "cmd_$cmd" "$@" ;;
	*) fail "Unknown command '$1'"; printf '\n' >&2; cmd_help >&2; exit 1 ;;
esac
