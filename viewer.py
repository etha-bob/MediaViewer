"""
MediaViewer - Main application window

Supports viewing common image formats (JPEG, PNG, GIF, BMP, WEBP, TIFF).
Requires Pillow: pip install Pillow
"""

import logging
import os
import tkinter as tk
from tkinter import filedialog, messagebox, ttk

logger = logging.getLogger(__name__)

# Image extensions supported (Pillow-backed)
IMAGE_EXTENSIONS = {
    ".jpg", ".jpeg", ".png", ".gif", ".bmp",
    ".webp", ".tiff", ".tif", ".ico",
}

try:
    from PIL import Image, ImageTk

    PILLOW_AVAILABLE = True
    logger.info("Pillow is available – full image support enabled")
except ImportError:
    PILLOW_AVAILABLE = False
    logger.warning(
        "Pillow is not installed. Install it with 'pip install Pillow' "
        "for full image support. Only native tkinter formats (GIF, PPM) "
        "will be displayed."
    )


class MediaViewerApp:
    """Main MediaViewer application."""

    def __init__(self, root: tk.Tk) -> None:
        self.root = root
        self.root.title("MediaViewer")
        self.root.geometry("900x650")
        self.root.minsize(400, 300)

        # State
        self._files: list[str] = []
        self._index: int = 0
        self._photo: object = None  # keep reference to avoid GC

        self._build_ui()
        self._bind_keys()

        logger.info("MediaViewerApp initialised")

    # ------------------------------------------------------------------
    # UI construction
    # ------------------------------------------------------------------

    def _build_ui(self) -> None:
        # Menu bar
        menubar = tk.Menu(self.root)
        file_menu = tk.Menu(menubar, tearoff=False)
        file_menu.add_command(label="Open File…", accelerator="Ctrl+O", command=self._open_file)
        file_menu.add_command(label="Open Folder…", accelerator="Ctrl+Shift+O", command=self._open_folder)
        file_menu.add_separator()
        file_menu.add_command(label="Exit", command=self.root.quit)
        menubar.add_cascade(label="File", menu=file_menu)
        self.root.config(menu=menubar)

        # Toolbar
        toolbar = tk.Frame(self.root, bd=1, relief=tk.RAISED)
        toolbar.pack(side=tk.TOP, fill=tk.X)

        tk.Button(toolbar, text="◀  Prev", command=self._prev).pack(side=tk.LEFT, padx=2, pady=2)
        tk.Button(toolbar, text="Next  ▶", command=self._next).pack(side=tk.LEFT, padx=2, pady=2)
        tk.Button(toolbar, text="Open File", command=self._open_file).pack(side=tk.LEFT, padx=6, pady=2)
        tk.Button(toolbar, text="Open Folder", command=self._open_folder).pack(side=tk.LEFT, padx=2, pady=2)

        # File counter label
        self._counter_var = tk.StringVar(value="No file loaded")
        tk.Label(toolbar, textvariable=self._counter_var).pack(side=tk.RIGHT, padx=8)

        # Canvas for image display
        self._canvas = tk.Canvas(self.root, bg="#1e1e1e", cursor="crosshair")
        self._canvas.pack(fill=tk.BOTH, expand=True)

        # Status bar
        self._status_var = tk.StringVar(value="Ready – open a file or folder to begin.")
        status_bar = tk.Label(
            self.root,
            textvariable=self._status_var,
            anchor=tk.W,
            relief=tk.SUNKEN,
            bd=1,
        )
        status_bar.pack(side=tk.BOTTOM, fill=tk.X)

    def _bind_keys(self) -> None:
        self.root.bind("<Control-o>", lambda _e: self._open_file())
        self.root.bind("<Control-Shift-o>", lambda _e: self._open_folder())
        self.root.bind("<Left>", lambda _e: self._prev())
        self.root.bind("<Right>", lambda _e: self._next())
        self.root.bind("<Configure>", self._on_resize)

    # ------------------------------------------------------------------
    # File / folder loading
    # ------------------------------------------------------------------

    def _open_file(self) -> None:
        path = filedialog.askopenfilename(
            title="Open media file",
            filetypes=[
                ("Image files", "*.jpg *.jpeg *.png *.gif *.bmp *.webp *.tiff *.tif *.ico"),
                ("All files", "*.*"),
            ],
        )
        if not path:
            return
        logger.info("User opened file: %s", path)
        self._files = [path]
        self._index = 0
        self._show_current()

    def _open_folder(self) -> None:
        folder = filedialog.askdirectory(title="Open folder")
        if not folder:
            return
        logger.info("User opened folder: %s", folder)
        self._files = sorted(
            os.path.join(folder, f)
            for f in os.listdir(folder)
            if os.path.splitext(f)[1].lower() in IMAGE_EXTENSIONS
        )
        if not self._files:
            messagebox.showinfo("No images found", f"No supported image files found in:\n{folder}")
            return
        self._index = 0
        self._show_current()

    # ------------------------------------------------------------------
    # Navigation
    # ------------------------------------------------------------------

    def _prev(self) -> None:
        if not self._files:
            return
        self._index = (self._index - 1) % len(self._files)
        self._show_current()

    def _next(self) -> None:
        if not self._files:
            return
        self._index = (self._index + 1) % len(self._files)
        self._show_current()

    # ------------------------------------------------------------------
    # Display
    # ------------------------------------------------------------------

    def _show_current(self) -> None:
        if not self._files:
            return
        path = self._files[self._index]
        logger.info("Displaying [%d/%d]: %s", self._index + 1, len(self._files), path)
        self._counter_var.set(f"{self._index + 1} / {len(self._files)}")
        self._status_var.set(os.path.basename(path))

        try:
            self._load_image(path)
        except Exception as exc:
            logger.error("Failed to display '%s': %s", path, exc)
            self._status_var.set(f"Error loading {os.path.basename(path)}: {exc}")
            self._canvas.delete("all")
            self._canvas.create_text(
                self._canvas.winfo_width() // 2 or 450,
                self._canvas.winfo_height() // 2 or 325,
                text=f"Cannot display this file.\n{exc}",
                fill="red",
                justify=tk.CENTER,
            )

    def _load_image(self, path: str) -> None:
        self._canvas.delete("all")
        ext = os.path.splitext(path)[1].lower()

        if PILLOW_AVAILABLE:
            img = Image.open(path)
            img = self._fit_image(img)
            self._photo = ImageTk.PhotoImage(img)
        elif ext in (".gif", ".ppm", ".pgm"):
            self._photo = tk.PhotoImage(file=path)
        else:
            raise RuntimeError(
                "Pillow is required to display this image format. "
                "Install it with: pip install Pillow"
            )

        cw = self._canvas.winfo_width() or 900
        ch = self._canvas.winfo_height() or 600
        self._canvas.create_image(cw // 2, ch // 2, anchor=tk.CENTER, image=self._photo)

    def _fit_image(self, img: "Image.Image") -> "Image.Image":
        """Scale the image to fit the current canvas while preserving aspect ratio."""
        cw = self._canvas.winfo_width() or 900
        ch = self._canvas.winfo_height() or 600
        iw, ih = img.size
        scale = min(cw / iw, ch / ih, 1.0)  # never upscale
        if scale < 1.0:
            new_w = max(1, int(iw * scale))
            new_h = max(1, int(ih * scale))
            img = img.resize((new_w, new_h), Image.LANCZOS)
        return img

    def _on_resize(self, _event: tk.Event) -> None:
        """Re-render the current image when the window is resized."""
        if self._files:
            self._show_current()
