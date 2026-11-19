import pytest
from app.services.optimization.tweak_service import TweakService


def test_tweaks_list_returns_list():
    tweaks = TweakService().get_all_tweaks()
    assert isinstance(tweaks, list)
