<?php
require __DIR__ . '/PromptRunner.php';

$runner = new PromptRunner();
$data = $runner->load();
$lastRun = $data['runs'] ? end($data['runs']) : null;
$message = '';
$newRun = null;
$rerunResult = null;

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? 'start';
    if ($action === 'start') {
        $promptsRaw = $_POST['prompts'] ?? '';
        $prompts = preg_split('/\r?\n/', (string)$promptsRaw);

        $resolution = $_POST['resolution'] ?? '720';

        $accountsInput = $_POST['accounts'] ?? [];
        $accounts = [];
        foreach ($accountsInput as $account) {
            if (!empty($account['name']) || !empty($account['cookie'])) {
                $accounts[] = [
                    'name' => trim($account['name']),
                    'cookie' => trim($account['cookie']),
                    'concurrency' => max(1, min(5, (int)($account['concurrency'] ?? 1))),
                ];
            }
        }

        $newRun = $runner->startRun($prompts, $accounts, $resolution);
        $lastRun = $newRun;
        $message = 'Đã chạy xong ' . count($prompts) . ' prompt.';
    } elseif ($action === 'download-all' && $lastRun) {
        $zipFile = sys_get_temp_dir() . '/' . $lastRun['id'] . '_videos.zip';
        $zip = new ZipArchive();
        if ($zip->open($zipFile, ZipArchive::CREATE | ZipArchive::OVERWRITE)) {
            foreach ($lastRun['prompts'] as $prompt) {
                if (isset($prompt['video_path']) && file_exists($prompt['video_path'])) {
                    $zip->addFile($prompt['video_path'], basename($prompt['video_path']));
                }
            }
            $zip->close();
            header('Content-Type: application/zip');
            header('Content-Disposition: attachment; filename="' . $lastRun['id'] . '_videos.zip"');
            readfile($zipFile);
            unlink($zipFile);
            exit;
        }
    } elseif ($action === 'download-one' && $lastRun) {
        $promptId = $_POST['prompt_id'] ?? '';
        foreach ($lastRun['prompts'] as $prompt) {
            if ($prompt['id'] === $promptId && isset($prompt['video_path']) && file_exists($prompt['video_path'])) {
                header('Content-Type: video/mp4');
                header('Content-Disposition: attachment; filename="' . $promptId . '.mp4"');
                readfile($prompt['video_path']);
                exit;
            }
        }
    } elseif ($action === 'rerun' && !empty($_POST['run_id']) && !empty($_POST['prompt_id'])) {
        $rerunResult = $runner->rerunPrompt($_POST['run_id'], $_POST['prompt_id']);
        if ($rerunResult) {
            $message = 'Đã chạy lại prompt: ' . htmlspecialchars($rerunResult['prompt'], ENT_QUOTES, 'UTF-8');
            $data = $runner->load();
            foreach ($data['runs'] as $run) {
                if ($run['id'] === $_POST['run_id']) {
                    $lastRun = $run;
                    break;
                }
            }
        }
    }
}

function h($value): string
{
    return htmlspecialchars((string)$value, ENT_QUOTES, 'UTF-8');
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title>Prompt Runner PHP</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background: #f6f6f6; }
        .card { background: #fff; padding: 16px; margin-bottom: 18px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.08); }
        label { display: block; font-weight: bold; margin-top: 8px; }
        textarea { width: 100%; min-height: 160px; }
        .accounts { margin-top: 8px; }
        .account-row { border: 1px solid #e1e1e1; padding: 8px; border-radius: 6px; margin-bottom: 8px; background: #fafafa; }
        .row { display: flex; gap: 8px; align-items: center; }
        .row input, .row select { flex: 1; padding: 6px; }
        .actions button { margin-right: 8px; padding: 10px 14px; }
        .prompt-list { list-style: none; padding: 0; }
        .prompt-list li { padding: 6px 0; border-bottom: 1px solid #efefef; display: flex; justify-content: space-between; align-items: center; }
        .badges { display: inline-flex; gap: 8px; }
        .badge { background: #eef; padding: 2px 8px; border-radius: 4px; font-size: 12px; color: #334; }
    </style>
    <script>
        function addAccountRow() {
            const container = document.getElementById('accounts');
            const index = container.children.length;
            const div = document.createElement('div');
            div.className = 'account-row';
            div.innerHTML = `
                <div class="row">
                    <input type="text" name="accounts[${index}][name]" placeholder="Tên tài khoản" />
                    <select name="accounts[${index}][concurrency]">
                        <option value="1">1 prompt</option>
                        <option value="2">2 prompt</option>
                        <option value="3">3 prompt</option>
                        <option value="4">4 prompt</option>
                        <option value="5">5 prompt</option>
                    </select>
                </div>
                <textarea name="accounts[${index}][cookie]" placeholder="Cookie đăng nhập Google Flow (tuỳ chọn)" rows="2"></textarea>
            `;
            container.appendChild(div);
        }
    </script>
</head>
<body>
    <h1>Quản lý chạy prompt (PHP)</h1>
    <div class="card">
        <form method="post">
            <input type="hidden" name="action" value="start" />
            <label>Prompt (mỗi dòng 1 prompt, tối đa 300)</label>
            <textarea name="prompts" placeholder="Nhập prompt tại đây..." required></textarea>

            <label>Độ phân giải</label>
            <select name="resolution">
                <option value="720">720p</option>
                <option value="1080">1080p</option>
            </select>

            <div class="accounts" id="accounts">
                <div class="account-row">
                    <div class="row">
                        <input type="text" name="accounts[0][name]" placeholder="Tên tài khoản" />
                        <select name="accounts[0][concurrency]">
                            <option value="1">1 prompt</option>
                            <option value="2">2 prompt</option>
                            <option value="3">3 prompt</option>
                            <option value="4">4 prompt</option>
                            <option value="5">5 prompt</option>
                        </select>
                    </div>
                    <textarea name="accounts[0][cookie]" placeholder="Cookie đăng nhập Google Flow (tuỳ chọn)" rows="2"></textarea>
                </div>
            </div>
            <button type="button" onclick="addAccountRow()">+ Thêm tài khoản</button>

            <div class="actions" style="margin-top:12px;">
                <button type="submit">Chạy prompt</button>
            </div>
        </form>
        <?php if ($message): ?>
            <p><strong><?php echo h($message); ?></strong></p>
        <?php endif; ?>
        <?php if ($rerunResult): ?>
            <div class="card">
                <h3>Kết quả chạy lại</h3>
                <pre><?php echo h(json_encode($rerunResult, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE)); ?></pre>
            </div>
        <?php endif; ?>
    </div>

    <?php if ($lastRun): ?>
    <div class="card">
        <h2>Chạy gần nhất</h2>
        <p>ID: <?php echo h($lastRun['id']); ?> | Thời gian: <?php echo h($lastRun['created_at']); ?> | Độ phân giải: <?php echo h($lastRun['resolution']); ?>p</p>
        <p>Tài khoản: <?php echo h(count($lastRun['accounts'])); ?> | Tổng prompt: <?php echo h(count($lastRun['prompts'])); ?></p>
        <form method="post" style="display:inline-block;">
            <input type="hidden" name="action" value="download-all" />
            <button type="submit">Tải xuống toàn bộ</button>
        </form>
        <ul class="prompt-list">
            <?php foreach ($lastRun['prompts'] as $prompt): ?>
                <li>
                    <div>
                        <div><strong><?php echo h($prompt['prompt']); ?></strong></div>
                        <div class="badges">
                            <span class="badge">Tài khoản: <?php echo h($prompt['account']); ?></span>
                            <span class="badge">Độ phân giải: <?php echo h($prompt['resolution']); ?>p</span>
                            <span class="badge">Thời gian: <?php echo h($prompt['duration_ms']); ?>ms</span>
                        </div>
                        <div>Kết quả: <?php echo h($prompt['result']); ?></div>
                        <?php if (!empty($prompt['video_path'])): ?>
                            <div>Tệp video: <?php echo h(basename($prompt['video_path'])); ?></div>
                        <?php endif; ?>
                    </div>
                    <div>
                        <form method="post" style="display:inline-block;">
                            <input type="hidden" name="action" value="download-one" />
                            <input type="hidden" name="prompt_id" value="<?php echo h($prompt['id']); ?>" />
                            <button type="submit">Tải prompt</button>
                        </form>
                        <form method="post" style="display:inline-block;">
                            <input type="hidden" name="action" value="rerun" />
                            <input type="hidden" name="run_id" value="<?php echo h($lastRun['id']); ?>" />
                            <input type="hidden" name="prompt_id" value="<?php echo h($prompt['id']); ?>" />
                            <button type="submit">Chạy lại</button>
                        </form>
                    </div>
                </li>
            <?php endforeach; ?>
        </ul>
    </div>
    <?php endif; ?>
</body>
</html>
