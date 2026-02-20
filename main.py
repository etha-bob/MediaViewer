"""
MediaViewer - Entry point

Launches the MediaViewer application and catches any startup errors,
writing them to log.txt so they can be diagnosed later.
"""

import logging
import os
import sys
import traceback

# ---------------------------------------------------------------------------
# Logging setup – write to log.txt next to the executable (or CWD)
# ---------------------------------------------------------------------------
LOG_PATH = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "log.txt")

logging.basicConfig(
    level=logging.DEBUG,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[
        logging.FileHandler(LOG_PATH, encoding="utf-8"),
        logging.StreamHandler(sys.stdout),
    ],
)

logger = logging.getLogger(__name__)


def main() -> None:
    logger.info("MediaViewer starting up")
    logger.info("Python %s", sys.version)
    logger.info("Executable: %s", sys.executable)
    logger.info("Working directory: %s", os.getcwd())

    try:
        import tkinter as tk  # noqa: PLC0415  (late import intentional)
    except ImportError:
        logger.critical(
            "tkinter is not available. On Windows this ships with Python. "
            "On Linux install python3-tk (e.g. 'sudo apt install python3-tk')."
        )
        sys.exit(1)

    try:
        from viewer import MediaViewerApp  # noqa: PLC0415
    except Exception:
        logger.critical("Failed to import viewer module:\n%s", traceback.format_exc())
        sys.exit(1)

    try:
        root = tk.Tk()
        _app = MediaViewerApp(root)
        logger.info("Window created – entering main loop")
        root.mainloop()
        logger.info("MediaViewer exited normally")
    except Exception:
        logger.critical("Unhandled exception during startup:\n%s", traceback.format_exc())
        sys.exit(1)


if __name__ == "__main__":
    main()
