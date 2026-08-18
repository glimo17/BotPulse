-- Seed a couple of failed jobs so the AI Analysis panel has something to diagnose.
-- Uses provider_name 'Demo' to match the local Demo RPA provider.

INSERT INTO jobs (
    external_job_id, provider_name, process_external_id, robot_external_id,
    machine_external_id, status, start_time_utc, end_time_utc, duration,
    error_type, error_message, created_at_utc, updated_at_utc)
VALUES
(
    'demo-job-fail-001', 'Demo', 'Finance.InvoiceProcessing', 'robot-fin-01',
    'machine-01', 'Failed',
    NOW() - INTERVAL '2 hours', NOW() - INTERVAL '2 hours' + INTERVAL '45 seconds', INTERVAL '45 seconds',
    'SelectorNotFoundException',
    'Could not find UI element with selector ''btnSubmitInvoice''. The target application window was not in the expected state.',
    NOW(), NOW()
),
(
    'demo-job-fail-002', 'Demo', 'HR.OnboardingBot', 'robot-hr-02',
    'machine-02', 'Failed',
    NOW() - INTERVAL '30 minutes', NOW() - INTERVAL '30 minutes' + INTERVAL '12 seconds', INTERVAL '12 seconds',
    'TimeoutException',
    'Timed out after 30000ms waiting for the employee record page to load.',
    NOW(), NOW()
)
ON CONFLICT (provider_name, external_job_id) DO NOTHING;

SELECT external_job_id, status, error_type FROM jobs WHERE provider_name = 'Demo';
