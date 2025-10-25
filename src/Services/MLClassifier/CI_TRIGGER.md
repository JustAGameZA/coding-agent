# ML Classifier Service CI Trigger

This file is used to trigger CI builds for the ML Classifier service.

Last updated: 2025-10-26

## Recent Changes
- Added training endpoints (/train/feedback, /train/retrain, /train/stats)
- Enhanced event listener documentation for TaskCompletedEvent
- Added GitHub Actions workflow with pytest and coverage enforcement (>=85%)
- Hybrid classifier fully operational with heuristic → ML → LLM cascade
