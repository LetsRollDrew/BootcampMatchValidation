# BootcampMatchValidation
Small project to fiddle around with C# and TFT/Twitch API

This tool takes in JSONs as shown under "Batch input format" and validates how many games a player, given a riotID with name#tag and twich LinkUrl, has played on stream, comparing timestamps from their match history to their VOD windows.

## Setup
- Install .NET 10 SDK.
- Copy `.env.example` to `.env` and fill in `RIOT_API_KEY`, `TWITCH_CLIENT_ID`, and `TWITCH_CLIENT_SECRET`.

## Build
```bash
dotnet build TftStreamChecker.sln
```

## Test
```bash
dotnet test TftStreamChecker.sln
```

## CLI flags
- `--riotId "name#tag"` and `--twitch login` (single participant)
- `--input file` (batch JSON; top 30 by Rank)
- `--outputCsv path` (empty to disable)
- `--concurrency N` (match detail fetch; default 1)
- `--noCache` (skip cache reads/writes)
- window flags: `--days`, `--startTime`, `--endTime`, `--eventYear`, `--eventStart`, `--eventEnd`
- `--threshold` (pass/fail percent; default 0.5)

## Run modes
- Single participant: pass `--riotId` and `--twitch`.
- Single JSON batch: `--input inputs/participants.json` (auto top-30 by Rank).
- Multi-JSON batch: `./scripts/stream-check-all.sh` (processes every `inputs/participants*.json`, writes `output/<file>-results-top30.csv`).

## Quick commands
- Single participant:
  ```bash
  ./run.sh --riotId "Player#NA1" --twitch playerchannel --outputCsv output/stream-check.csv --concurrency 1
  ```
- One JSON file:
  ```bash
  ./run.sh --input inputs/participants.json --outputCsv output/participants-results-top30.csv --concurrency 1
  ```
- All JSONs in `inputs/`:
  ```bash
  ./scripts/stream-check-all.sh
  ```

## Defaults/toggles:
- `--concurrency` defaults to 1 (best with basic Riot key)
- `--outputCsv ""` disables CSV.
- `--noCache` skips `.cache` reads/writes.
- Riot 404 for Name Change catches and a zeroed CSV row.

## Batch input format
- Format is based off boxboxtft.com content served as so:
- JSON can be an array of participants like`{ "Participants": [ ... ] }`.
- Each participant uses fields like:
```json
{
  "Team": "pro",
  "Rank": 1,
  "Name": "DOGDOG",
  "Eliminated": false,
  "PotentiallyEliminated": false,
  "DayEliminated": 2,
  "Socials": [
    { "LinkUri": "https://twitch.tv/DOGDOG" }
  ],
  "RankUrl": "https://tft.op.gg/summoners/na/dogdogdog-na1"
}
```
- `RankUrl` is parsed for RiotId,
- `Socials[].LinkUri` for twitch login,
- `Eliminated`/`DayEliminated` clamps a window of time
