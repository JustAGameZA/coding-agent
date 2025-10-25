"""Example usage of the ML Classifier."""

import asyncio

from api.schemas.classification import ClassificationRequest
from domain.classifiers.ml_classifier import MLClassifier


async def main():
    """Demonstrate ML classifier usage."""
    print("Loading ML Classifier...")
    classifier = MLClassifier.load_from_disk()
    
    print(f"Model Version: {classifier.get_model_version()}")
    print(f"Model Accuracy: {classifier.get_model_accuracy():.1%}")
    print(f"Feature Count: {classifier.feature_extractor.feature_count}")
    print()
    
    # Test various task descriptions
    test_cases = [
        "Fix the login bug where users can't authenticate",
        "Implement a new user registration feature with email verification",
        "Refactor the authentication module to improve code quality",
        "Write unit tests for the user service with 90% coverage",
        "Update the README with installation instructions and examples",
        "Deploy the application to Kubernetes cluster with Helm charts",
        "Implement a complex microservices architecture with API gateway",
    ]
    
    print("Classification Results:")
    print("=" * 80)
    
    for description in test_cases:
        request = ClassificationRequest(task_description=description)
        result = await classifier.classify(request)
        
        print(f"\nTask: {description[:60]}...")
        print(f"  Type: {result.task_type.value}")
        print(f"  Complexity: {result.complexity.value}")
        print(f"  Confidence: {result.confidence:.1%}")
        print(f"  Strategy: {result.suggested_strategy}")
        print(f"  Est. Tokens: {result.estimated_tokens:,}")
        print(f"  Reasoning: {result.reasoning[:100]}...")


if __name__ == "__main__":
    asyncio.run(main())
