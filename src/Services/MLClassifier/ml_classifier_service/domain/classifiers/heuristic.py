"""Heuristic classifier using keyword matching for fast classification."""

import re
from typing import Dict, List

from api.schemas.classification import ClassificationResult
from domain.models.task_type import TaskComplexity, TaskType


class HeuristicClassifier:
    """Fast keyword-based classification (90% accuracy, 5ms latency)."""

    # Keyword patterns for each task type
    KEYWORDS: Dict[TaskType, List[str]] = {
        TaskType.BUG_FIX: [
            r"\bbug\b",
            r"\berror\b",
            r"\bfix\b",
            r"\bcrash\b",
            r"\bissue\b",
            r"\bfail(s|ing|ed)?\b",
            r"\bbroken\b",
            r"\bdefect\b",
            r"\bproblem\b",
            r"\bincorrect\b",
        ],
        TaskType.FEATURE: [
            r"\badd\b",
            r"\bimplement\b",
            r"\bcreate\b",
            r"\bnew\b",
            r"\bfeature\b",
            r"\benhance\b",
            r"\bsupport\b",
            r"\bintroduce\b",
            r"\bextend\b",
            r"\bbuild\b",
        ],
        TaskType.REFACTOR: [
            r"\brefactor\b",
            r"\bclean\b",
            r"\boptimize\b",
            r"\bimprove\b",
            r"\breorganize\b",
            r"\brestructure\b",
            r"\bsimplify\b",
            r"\bmodernize\b",
            r"\bupgrade\b",
        ],
        TaskType.TEST: [
            r"\btest\b",
            r"\bunit test\b",
            r"\bintegration test\b",
            r"\bcoverage\b",
            r"\bspec\b",
            r"\bvalidate\b",
            r"\bverify\b",
            r"\bmock\b",
            r"\bassertion\b",
        ],
        TaskType.DOCUMENTATION: [
            r"\bdoc(s|umentation)?\b",
            r"\breadme\b",
            r"\bcomment\b",
            r"\bexplain\b",
            r"\bdescribe\b",
            r"\bguide\b",
            r"\btutorial\b",
            r"\bexample\b",
            r"\bannotate\b",
        ],
        TaskType.DEPLOYMENT: [
            r"\bdeploy\b",
            r"\brelease\b",
            r"\bci/cd\b",
            r"\bpipeline\b",
            r"\bdocker\b",
            r"\bkubernetes\b",
            r"\bhelm\b",
            r"\bcontainer\b",
            r"\binfrastructure\b",
        ],
    }

    # Complexity indicators
    COMPLEXITY_KEYWORDS = {
        "simple": [
            r"\bsmall\b",
            r"\bquick\b",
            r"\bminor\b",
            r"\btrivial\b",
            r"\btypo\b",
            r"\bone[ -]line\b",
            r"\bsimple\b",
        ],
        "complex": [
            r"\bcomplex\b",
            r"\bmajor\b",
            r"\barchitecture\b",
            r"\brewrite\b",
            r"\bmigration\b",
            r"\brefactor all\b",
            r"\blarge[ -]scale\b",
            r"\bentire\b",
            r"\bsystem[ -]wide\b",
        ],
    }

    def __init__(self):
        """Initialize the heuristic classifier with compiled regex patterns."""
        # Compile regex patterns for performance
        self.compiled_keywords = {
            task_type: [re.compile(pattern, re.IGNORECASE) for pattern in patterns]
            for task_type, patterns in self.KEYWORDS.items()
        }
        self.compiled_complexity = {
            level: [re.compile(pattern, re.IGNORECASE) for pattern in patterns]
            for level, patterns in self.COMPLEXITY_KEYWORDS.items()
        }

    def classify(self, task_description: str) -> ClassificationResult:
        """
        Classify task using keyword matching.

        Args:
            task_description: The description of the task to classify

        Returns:
            ClassificationResult with task type, complexity, and confidence
        """
        # Count keyword matches per task type
        match_counts = {}
        for task_type, patterns in self.compiled_keywords.items():
            count = sum(1 for pattern in patterns if pattern.search(task_description))
            if count > 0:
                match_counts[task_type] = count

        # No matches - default to FEATURE with low confidence
        if not match_counts:
            complexity = self._classify_complexity(task_description)
            return ClassificationResult(
                task_type=TaskType.FEATURE,
                complexity=complexity,
                confidence=0.3,
                reasoning="No keyword matches found, defaulting to FEATURE",
                suggested_strategy=self._suggest_strategy(complexity),
                estimated_tokens=self._estimate_tokens(complexity),
                classifier_used="heuristic",
            )

        # Get task type with most matches
        predicted_type = max(match_counts, key=match_counts.get)
        max_matches = match_counts[predicted_type]

        # Calculate confidence based on match count and uniqueness
        total_matches = sum(match_counts.values())
        base_confidence = max_matches / total_matches if total_matches > 0 else 0

        # Boost confidence if matches are unique to one type
        if len(match_counts) == 1:
            confidence = min(0.95, base_confidence + 0.2)
        else:
            confidence = min(0.85, base_confidence)

        complexity = self._classify_complexity(task_description)

        # Build reasoning message
        matched_patterns = [
            pattern.pattern
            for pattern in self.compiled_keywords[predicted_type]
            if pattern.search(task_description)
        ]
        reasoning = f"Matched {max_matches} keywords for {predicted_type.value}: {', '.join(matched_patterns[:3])}"

        return ClassificationResult(
            task_type=predicted_type,
            complexity=complexity,
            confidence=confidence,
            reasoning=reasoning,
            suggested_strategy=self._suggest_strategy(complexity),
            estimated_tokens=self._estimate_tokens(complexity),
            classifier_used="heuristic",
        )

    def _classify_complexity(self, description: str) -> TaskComplexity:
        """
        Classify complexity based on indicators.

        Args:
            description: Task description

        Returns:
            TaskComplexity enum value
        """
        # Check for explicit complexity keywords
        simple_matches = sum(
            1
            for pattern in self.compiled_complexity["simple"]
            if pattern.search(description)
        )
        complex_matches = sum(
            1
            for pattern in self.compiled_complexity["complex"]
            if pattern.search(description)
        )

        if complex_matches > 0:
            return TaskComplexity.COMPLEX
        elif simple_matches > 0:
            return TaskComplexity.SIMPLE

        # Use length as heuristic
        word_count = len(description.split())
        if word_count < 20:
            return TaskComplexity.SIMPLE
        elif word_count > 100:
            return TaskComplexity.COMPLEX
        else:
            return TaskComplexity.MEDIUM

    def _suggest_strategy(self, complexity: TaskComplexity) -> str:
        """
        Suggest execution strategy based on complexity.

        Args:
            complexity: Task complexity level

        Returns:
            Strategy name as string
        """
        return {
            TaskComplexity.SIMPLE: "SingleShot",
            TaskComplexity.MEDIUM: "Iterative",
            TaskComplexity.COMPLEX: "MultiAgent",
        }[complexity]

    def _estimate_tokens(self, complexity: TaskComplexity) -> int:
        """
        Estimate token usage based on complexity.

        Args:
            complexity: Task complexity level

        Returns:
            Estimated token count
        """
        return {
            TaskComplexity.SIMPLE: 2000,
            TaskComplexity.MEDIUM: 6000,
            TaskComplexity.COMPLEX: 20000,
        }[complexity]
