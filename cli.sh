#!/usr/bin/env bash
# Chess Bot developer CLI, driven by the Makefile. Run `./cli.sh help` for usage.

set -uo pipefail
cd "$(dirname "$0")"

SOLUTION=Chess-Bot.sln
PROJECT=Chess-Bot.csproj
CONFIG=${CONFIG:-Release}
DIST_DIR=${DIST_DIR:-dist}
VERSION_FILE=$PROJECT
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
  ${A}check${R}     Build everything and test           
  ${A}format${R}    Fix whitespace to match .editorconfig

${B}Project${R}
  ${A}version${R}   Print the current version
  ${A}bump${R}      Bump the version                    

${D}Without RID, publish opens a picker.${R}
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
	rule
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

	local out=$DIST_DIR/chess-bot-$(current_version)-$rid
	rm -rf "$out"
	step "Publishing $rid" "Published $rid $DIM→ $out/$RESET" \
		dotnet publish "$PROJECT" -c Release -r "$rid" --self-contained -nologo -o "$out" || exit 1

	# The app loads fonts, sprites, sounds and openings from Chess/Resources next to where it runs
	mkdir -p "$out/Chess"
	cp -r Chess/Resources "$out/Chess/"
	note "Run it from inside that folder so it can find Chess/Resources"
}

cmd_clean() {
	rm -rf bin obj Tests/bin Tests/obj TestResults "$DIST_DIR"
	ok "Removed bin/, obj/, Tests/bin/, Tests/obj/ and $DIST_DIR/"
}

# The <Version> in Chess-Bot.csproj.
current_version() { sed -n 's/^ *<Version>\([^<]*\)<\/Version>.*/\1/p' "$VERSION_FILE" | head -1; }

cmd_version() { current_version; }

cmd_bump() {
	local to=${1:-patch} old new major minor patch
	old=$(current_version)
	if [[ ! $old =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
		fail "No X.Y.Z <Version> in $VERSION_FILE"; exit 1
	fi
	IFS=. read -r major minor patch <<<"$old"
	case $to in
		major) new="$((major + 1)).0.0" ;;
		minor) new="$major.$((minor + 1)).0" ;;
		patch) new="$major.$minor.$((patch + 1))" ;;
		*)
			if [[ ! $to =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
				fail "TO must be patch, minor, major or X.Y.Z, got '$to'"; exit 1
			fi
			new=$to ;;
	esac
	sed "s/<Version>$old<\/Version>/<Version>$new<\/Version>/" "$VERSION_FILE" >"$VERSION_FILE.tmp" \
		&& mv "$VERSION_FILE.tmp" "$VERSION_FILE"
	ok "Bumped version $DIM$old → $new$RESET"
}

case ${1:-help} in
	help|install|build|run|publish|clean|test|check|format|version|bump) cmd=$1; shift; "cmd_$cmd" "$@" ;;
	*) fail "Unknown command '$1'"; printf '\n' >&2; cmd_help >&2; exit 1 ;;
esac
