<?php
class PromptRunner
{
    private string $dataFile;
    private string $videoDir;

    public function __construct(?string $dataFile = null)
    {
        $this->dataFile = $dataFile ?? __DIR__ . '/data/runs.json';
        $dataDir = dirname($this->dataFile);
        $this->videoDir = $dataDir . '/videos';

        if (!is_dir($dataDir)) {
            mkdir($dataDir, 0777, true);
        }
        if (!is_dir($this->videoDir)) {
            mkdir($this->videoDir, 0777, true);
        }
        if (!file_exists($this->dataFile)) {
            $this->persist(['runs' => []]);
        }
    }

    public function load(): array
    {
        $json = file_get_contents($this->dataFile);
        if ($json === false) {
            return ['runs' => []];
        }
        $data = json_decode($json, true);
        return is_array($data) ? $data : ['runs' => []];
    }

    public function persist(array $data): void
    {
        file_put_contents($this->dataFile, json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
    }

    public function startRun(array $prompts, array $accounts, string $resolution): array
    {
        $prompts = array_values(array_filter(array_map('trim', $prompts), fn ($p) => $p !== ''));
        if (!$prompts) {
            return [];
        }

        $runId = uniqid('run_', true);
        $run = [
            'id' => $runId,
            'created_at' => date('c'),
            'resolution' => $resolution,
            'accounts' => $accounts,
            'prompts' => [],
        ];

        $accountLoad = [];
        foreach ($accounts as $index => $account) {
            $accountLoad[$index] = 0;
        }

        foreach ($prompts as $promptIndex => $promptText) {
            $accountIndex = $this->pickAccount($accounts, $accountLoad);
            $accountLoad[$accountIndex] += 1;
            $run['prompts'][] = $this->executePrompt($runId, $promptText, $resolution, $accounts[$accountIndex], $promptIndex + 1);
        }

        $data = $this->load();
        $data['runs'][] = $run;
        $this->persist($data);

        return $run;
    }

    public function rerunPrompt(string $runId, string $promptId): ?array
    {
        $data = $this->load();
        foreach ($data['runs'] as $run) {
            if ($run['id'] !== $runId) {
                continue;
            }
            foreach ($run['prompts'] as $prompt) {
                if ($prompt['id'] !== $promptId) {
                    continue;
                }
                // Use the first account or fallback to default
                $account = $run['accounts'][0] ?? ['name' => 'default', 'concurrency' => 1, 'cookie' => ''];
                $newResult = $this->executePrompt($runId, $prompt['prompt'], $run['resolution'], $account, 1);
                $prompt['result'] = $newResult['result'];
                $prompt['video_path'] = $newResult['video_path'];
                $prompt['duration_ms'] = $newResult['duration_ms'];

                $updatedRuns = $this->load();
                foreach ($updatedRuns['runs'] as &$storedRun) {
                    if ($storedRun['id'] !== $runId) {
                        continue;
                    }
                    foreach ($storedRun['prompts'] as &$storedPrompt) {
                        if ($storedPrompt['id'] === $promptId) {
                            $storedPrompt = $prompt;
                            break;
                        }
                    }
                }
                $this->persist($updatedRuns);

                return $prompt;
            }
        }
        return null;
    }

    private function pickAccount(array $accounts, array $accountLoad): int
    {
        if (!$accounts) {
            return 0;
        }
        $bestIndex = 0;
        $bestScore = PHP_INT_MAX;
        foreach ($accounts as $index => $account) {
            $concurrency = max(1, min(5, (int)($account['concurrency'] ?? 1)));
            $score = $accountLoad[$index] / $concurrency;
            if ($score < $bestScore) {
                $bestScore = $score;
                $bestIndex = $index;
            }
        }
        return $bestIndex;
    }

    private function executePrompt(string $runId, string $prompt, string $resolution, array $account, int $sequence): array
    {
        // This is a placeholder for an external call. Here we simulate processing.
        $start = microtime(true);
        // Simulate workload depending on resolution to reflect heavier processing.
        usleep(($resolution === '1080' ? 50000 : 30000));

        $resultText = sprintf(
            'Account "%s" processed prompt #%d (%s): %s',
            $account['name'] ?: 'default',
            $sequence,
            $resolution . 'p',
            substr(hash('sha256', $prompt . $resolution), 0, 16)
        );

        $promptId = uniqid('prompt_', true);
        $videoPath = $this->writeVideoFile($runId, $promptId, $resolution);

        return [
            'id' => $promptId,
            'prompt' => $prompt,
            'account' => $account['name'] ?? 'default',
            'resolution' => $resolution,
            'duration_ms' => (int)((microtime(true) - $start) * 1000),
            'result' => $resultText,
            'video_path' => $videoPath,
        ];
    }

    private function writeVideoFile(string $runId, string $promptId, string $resolution): string
    {
        $runDir = $this->videoDir . '/' . $runId;
        if (!is_dir($runDir)) {
            mkdir($runDir, 0777, true);
        }

        $filePath = sprintf('%s/%s_%sp.mp4', $runDir, $promptId, $resolution);
        file_put_contents($filePath, $this->placeholderVideoBytes());

        return $filePath;
    }

    private function placeholderVideoBytes(): string
    {
        // A tiny black MP4 clip (base64 encoded) to act as downloadable output.
        $base64 = 'AAAAIGZ0eXBpc29tAAAAAGlzb21pc28ybXA0MQAAAwVtb292AAAAbG12aGQAAAAAAAAAAAAAAAAAAAPoAAAAAAABAAABAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAABR0cmFrAAAAXHRraGQAAAAAAAAAAAIAAAYAAAAAAAEAAAABAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADwAAAAAAQAAAG1kaWEAAAAgbWRoZAAAAAAAAAAAAAAAAAAAA+gAAAAAAAEAAAAAAAAAAAAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAQAAAAAAFIHVkdGEAAAArdWR0YQAAACtkYXRhAAAAAQAAAACEi1EAAAAAAAACGF2YzEAAAAAU2hhcm1hZjQ3LjI5LjAw';
        return base64_decode($base64);
    }
}
